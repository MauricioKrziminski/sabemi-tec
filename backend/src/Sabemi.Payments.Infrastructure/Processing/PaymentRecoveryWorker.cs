using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Core.Processing;
using Sabemi.Payments.Infrastructure.Persistence;

namespace Sabemi.Payments.Infrastructure.Processing;

/// <summary>
/// Varredura que garante a entrega mesmo sem broker externo.
///
/// O canal em memória é apenas um atalho para evitar latência de polling. A fila de verdade é a
/// tabela de eventos brutos, e é esta varredura que fecha as janelas em que o sinal se perde:
/// processo derrubado entre o commit e o enfileiramento, canal cheio, evento reservado por uma
/// instância que morreu, ou tentativa aguardando o fim do backoff.
/// </summary>
public sealed class PaymentRecoveryWorker(
    IDbContextFactory<PaymentsDbContext> contextFactory,
    IPaymentEventQueue queue,
    IOptions<ProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<PaymentRecoveryWorker> logger) : BackgroundService
{
    private readonly ProcessingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.RecoveryInterval, timeProvider);

        // A primeira passada acontece na inicialização, recuperando o que ficou de um restart.
        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha na varredura de recuperação de eventos.");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var abandonedBefore = now - _options.StuckTimeout;

        // Eventos reservados por uma instância que não concluiu o trabalho.
        var exhausted = await context.WebhookEventLogs
            .Where(log => log.Status == EventProcessingStatus.Processing
                && log.ProcessingStartedAt < abandonedBefore
                && log.Attempts >= _options.MaxAttempts)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(log => log.Status, EventProcessingStatus.PermanentlyFailed)
                    .SetProperty(log => log.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.ErrorMessage, "Processamento interrompido e tentativas esgotadas."),
                cancellationToken);

        var released = await context.WebhookEventLogs
            .Where(log => log.Status == EventProcessingStatus.Processing
                && log.ProcessingStartedAt < abandonedBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(log => log.Status, EventProcessingStatus.Failed)
                    .SetProperty(log => log.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(log => log.NextAttemptAt, now)
                    .SetProperty(log => log.ErrorMessage, "Processamento interrompido antes da conclusão."),
                cancellationToken);

        var pending = await context.WebhookEventLogs
            .Where(log => (log.Status == EventProcessingStatus.Pending
                    || log.Status == EventProcessingStatus.Failed)
                && (log.NextAttemptAt == null || log.NextAttemptAt <= now))
            .OrderBy(log => log.ReceivedAt)
            .Take(_options.RecoveryBatchSize)
            .Select(log => log.Id)
            .ToListAsync(cancellationToken);

        foreach (var eventId in pending)
        {
            queue.TryEnqueue(eventId);
        }

        if (exhausted + released + pending.Count > 0)
        {
            logger.LogInformation(
                "Varredura concluída. Reenfileirados: {Pending}. Liberados: {Released}. Encerrados: {Exhausted}.",
                pending.Count,
                released,
                exhausted);
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
