using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Infrastructure.Persistence;

namespace Sabemi.Payments.Infrastructure.Processing;

public enum ReprocessResult
{
    NotFound,

    /// <summary>O evento está em um estado que não admite nova tentativa manual.</summary>
    NotAllowed,

    Requeued
}

/// <summary>
/// Reenfileira manualmente um evento que falhou. É o que dá função operacional ao alerta
/// exibido no painel: quem vê o erro consegue agir sobre ele.
/// </summary>
public sealed class PaymentReprocessService(
    IDbContextFactory<PaymentsDbContext> contextFactory,
    IPaymentEventQueue queue,
    IPaymentEventNotifier notifier,
    TimeProvider timeProvider,
    ILogger<PaymentReprocessService> logger)
{
    public async Task<ReprocessResult> ReprocessAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var eventLog = await context.WebhookEventLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == eventId, cancellationToken);

        if (eventLog is null)
        {
            return ReprocessResult.NotFound;
        }

        // Um payload reprovado na validação continua reprovado, então reprocessar não faria sentido.
        if (eventLog.Status is not (EventProcessingStatus.Failed or EventProcessingStatus.PermanentlyFailed))
        {
            return ReprocessResult.NotAllowed;
        }

        var now = timeProvider.GetUtcNow();

        await context.WebhookEventLogs
            .Where(log => log.Id == eventId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(log => log.Status, EventProcessingStatus.Pending)
                    .SetProperty(log => log.Attempts, (short)0)
                    .SetProperty(log => log.NextAttemptAt, now)
                    .SetProperty(log => log.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.ErrorMessage, (string?)null),
                cancellationToken);

        queue.TryEnqueue(eventId);

        logger.LogInformation("Evento {TransactionId} reenfileirado manualmente.", eventLog.TransactionId);

        var updated = await context.WebhookEventLogs
            .AsNoTracking()
            .FirstAsync(log => log.Id == eventId, cancellationToken);

        await notifier.EventUpdatedAsync(PaymentEventDto.From(updated), cancellationToken);

        return ReprocessResult.Requeued;
    }
}
