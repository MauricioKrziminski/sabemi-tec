using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Infrastructure.Persistence;

namespace Sabemi.Payments.Infrastructure.Processing;

public sealed class PaymentEventProcessor(
    IDbContextFactory<PaymentsDbContext> contextFactory,
    IPaymentEventNotifier notifier,
    IOptions<ProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<PaymentEventProcessor> logger)
{
    private const int MaxErrorMessageLength = 500;

    private readonly ProcessingOptions _options = options.Value;

    public async Task ProcessAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var eventLog = await ClaimAsync(context, eventId, cancellationToken);
        if (eventLog is null)
        {
            return;
        }

        await NotifyAsync(eventLog, cancellationToken);

        try
        {
            await Task.Delay(_options.SimulatedWorkDelay, timeProvider, cancellationToken);

            await ApplyAsync(context, eventLog, cancellationToken);

            logger.LogInformation(
                "Evento {TransactionId} processado em {Attempts} tentativa(s).",
                eventLog.TransactionId,
                eventLog.Attempts);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(context, eventLog, exception, cancellationToken);
        }

        await ReloadAndNotifyAsync(context, eventId, cancellationToken);
    }

    private static async Task<WebhookEventLog?> ClaimAsync(
        PaymentsDbContext context,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var claimed = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE webhook_event_logs
             SET status = 'Processing',
                 processing_started_at = now(),
                 attempts = attempts + 1
             WHERE id = (
                 SELECT id
                 FROM webhook_event_logs
                 WHERE id = {eventId}
                   AND status IN ('Pending', 'Failed')
                 FOR UPDATE SKIP LOCKED
             )
             """,
            cancellationToken);

        if (claimed == 0)
        {
            return null;
        }

        return await context.WebhookEventLogs
            .AsNoTracking()
            .FirstAsync(log => log.Id == eventId, cancellationToken);
    }

    private async Task ApplyAsync(
        PaymentsDbContext context,
        WebhookEventLog eventLog,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await UpsertContractAsync(context, eventLog, cancellationToken);

        await context.WebhookEventLogs
            .Where(log => log.Id == eventLog.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(log => log.Status, EventProcessingStatus.Processed)
                    .SetProperty(log => log.ProcessedAt, timeProvider.GetUtcNow())
                    .SetProperty(log => log.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.ErrorMessage, (string?)null),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task UpsertContractAsync(
        PaymentsDbContext context,
        WebhookEventLog eventLog,
        CancellationToken cancellationToken)
    {
        var contractId = eventLog.ContractId!;
        var paymentDate = eventLog.PaymentDate!.Value;
        var settled = PaymentOutcomes.TryParse(eventLog.PaymentStatus, out var outcome)
            && outcome == PaymentOutcome.Success;

        var amount = settled ? eventLog.Amount!.Value : 0m;
        var count = settled ? 1 : 0;

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO contract_statuses (
                 contract_id, last_status, last_transaction_id, last_payment_date,
                 total_paid, payment_count, created_at, updated_at)
             VALUES (
                 {contractId}, {eventLog.PaymentStatus}, {eventLog.TransactionId}, {paymentDate},
                 {amount}, {count}, now(), now())
             ON CONFLICT (contract_id) DO UPDATE SET
                 total_paid = contract_statuses.total_paid + EXCLUDED.total_paid,
                 payment_count = contract_statuses.payment_count + EXCLUDED.payment_count,
                 last_status = CASE
                     WHEN EXCLUDED.last_payment_date >= contract_statuses.last_payment_date
                     THEN EXCLUDED.last_status
                     ELSE contract_statuses.last_status END,
                 last_transaction_id = CASE
                     WHEN EXCLUDED.last_payment_date >= contract_statuses.last_payment_date
                     THEN EXCLUDED.last_transaction_id
                     ELSE contract_statuses.last_transaction_id END,
                 last_payment_date = GREATEST(
                     contract_statuses.last_payment_date, EXCLUDED.last_payment_date),
                 updated_at = now()
             """,
            cancellationToken);
    }

    private async Task HandleFailureAsync(
        PaymentsDbContext context,
        WebhookEventLog eventLog,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exhausted = RetryPolicy.IsExhausted(eventLog.Attempts, _options.MaxAttempts);
        var status = exhausted ? EventProcessingStatus.PermanentlyFailed : EventProcessingStatus.Failed;

        DateTimeOffset? nextAttemptAt = exhausted
            ? null
            : timeProvider.GetUtcNow() + RetryPolicy.DelayFor(eventLog.Attempts, _options.InitialRetryDelay);

        logger.LogError(
            exception,
            "Falha ao processar o evento {TransactionId} na tentativa {Attempt}. Novo estado: {Status}.",
            eventLog.TransactionId,
            eventLog.Attempts,
            status);

        await context.WebhookEventLogs
            .Where(log => log.Id == eventLog.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(log => log.Status, status)
                    .SetProperty(log => log.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.NextAttemptAt, nextAttemptAt)
                    .SetProperty(log => log.ErrorMessage, Truncate(exception.Message)),
                cancellationToken);
    }

    private async Task ReloadAndNotifyAsync(
        PaymentsDbContext context,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var current = await context.WebhookEventLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == eventId, cancellationToken);

        if (current is not null)
        {
            await NotifyAsync(current, cancellationToken);
        }
    }

    private async Task NotifyAsync(WebhookEventLog eventLog, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.EventUpdatedAsync(PaymentEventDto.From(eventLog), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Não foi possível notificar o painel sobre o evento {EventId}.", eventLog.Id);
        }
    }

    private static string Truncate(string message) =>
        message.Length <= MaxErrorMessageLength ? message : message[..MaxErrorMessageLength];
}
