using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Infrastructure.Persistence;

namespace Sabemi.Payments.Infrastructure.Ingestion;

public sealed class WebhookIngestionService(
    IDbContextFactory<PaymentsDbContext> contextFactory,
    IValidator<PaymentWebhookRequest> validator,
    IPaymentEventQueue queue,
    IPaymentEventNotifier notifier,
    TimeProvider timeProvider,
    ILogger<WebhookIngestionService> logger)
{
    private const int MaxTransactionIdLength = 100;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<IngestionOutcome> IngestAsync(
        WebhookIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParsePayload(request.RawBody, out var payloadObject, out var parseError))
        {
            return new IngestionOutcome.Unparseable(parseError);
        }

        var payloadHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.RawBody));
        var transactionId = ExtractTransactionId(payloadObject, payloadHash);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.WebhookEventLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.TransactionId == transactionId, cancellationToken);

        if (existing is not null)
        {
            return await HandleDuplicateAsync(existing, payloadHash, cancellationToken);
        }

        var errors = Validate(payloadObject, out var payment);
        var receivedAt = timeProvider.GetUtcNow();
        var isValid = errors.Count == 0;

        var eventLog = new WebhookEventLog
        {
            TransactionId = transactionId,
            ContractId = Trim(payment?.ContractId, 50),
            Amount = payment?.Amount,
            PaymentDate = payment?.PaymentDate?.ToUniversalTime(),
            PaymentStatus = Trim(payment?.Status?.ToLowerInvariant(), 20),
            Payload = request.RawBody,
            PayloadHash = payloadHash,
            Headers = request.Headers,
            Status = isValid ? EventProcessingStatus.Pending : EventProcessingStatus.Invalid,
            ErrorMessage = isValid ? null : string.Join(" ", errors),
            NextAttemptAt = isValid ? receivedAt : null,
            ReceivedAt = receivedAt,
            CorrelationId = Trim(request.CorrelationId, 64)
        };

        context.WebhookEventLogs.Add(eventLog);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await using var freshContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            var conflicting = await freshContext.WebhookEventLogs
                .AsNoTracking()
                .FirstAsync(log => log.TransactionId == transactionId, cancellationToken);

            return await HandleDuplicateAsync(conflicting, payloadHash, cancellationToken);
        }

        await notifier.EventReceivedAsync(PaymentEventDto.From(eventLog), cancellationToken);

        if (isValid)
        {
            queue.TryEnqueue(eventLog.Id);
            return new IngestionOutcome.Accepted(eventLog.Id);
        }

        logger.LogInformation(
            "Evento {TransactionId} rejeitado na validação: {Errors}",
            transactionId,
            eventLog.ErrorMessage);

        return new IngestionOutcome.Rejected(eventLog.Id, errors);
    }

    private List<string> Validate(JsonObject payloadObject, out PaymentWebhookRequest? payment)
    {
        payment = null;
        var errors = new List<string>();

        try
        {
            payment = payloadObject.Deserialize<PaymentWebhookRequest>(SerializerOptions);
        }
        catch (JsonException)
        {
            errors.Add("O payload possui campos com tipo incompatível com o contrato acordado.");
            return errors;
        }

        if (payment is null)
        {
            errors.Add("O payload não pôde ser interpretado.");
            return errors;
        }

        var result = validator.Validate(payment);
        if (!result.IsValid)
        {
            errors.AddRange(result.Errors.Select(failure => failure.ErrorMessage));
        }

        return errors;
    }

    private async Task<IngestionOutcome> HandleDuplicateAsync(
        WebhookEventLog existing,
        byte[] payloadHash,
        CancellationToken cancellationToken)
    {
        var diverges = !CryptographicOperations.FixedTimeEquals(existing.PayloadHash, payloadHash);

        if (diverges && !existing.HasPayloadDivergence)
        {
            logger.LogWarning(
                "Reenvio do evento {TransactionId} com corpo diferente do original.",
                existing.TransactionId);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.WebhookEventLogs
                .Where(log => log.Id == existing.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(log => log.HasPayloadDivergence, true),
                    cancellationToken);
        }

        return new IngestionOutcome.Duplicate(existing.Id, existing.Status, diverges);
    }

    private static bool TryParsePayload(string rawBody, out JsonObject payload, out string error)
    {
        payload = [];

        try
        {
            if (JsonNode.Parse(rawBody) is JsonObject parsed)
            {
                payload = parsed;
                error = string.Empty;
                return true;
            }

            error = "O corpo da requisição deve ser um objeto JSON.";
            return false;
        }
        catch (JsonException)
        {
            error = "O corpo da requisição não é um JSON válido.";
            return false;
        }
    }

    private static string ExtractTransactionId(JsonObject payload, byte[] payloadHash)
    {
        if (payload.TryGetPropertyValue("id_transacao", out var node) && node is JsonValue value)
        {
            var text = value.GetValueKind() switch
            {
                JsonValueKind.String => value.GetValue<string>(),
                JsonValueKind.Number => value.ToJsonString(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                return Trim(text, MaxTransactionIdLength)!;
            }
        }

        return $"sem-id-{Convert.ToHexStringLower(payloadHash)[..16]}";
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public sealed record WebhookIngestionRequest(string RawBody, string? Headers, string? CorrelationId);
