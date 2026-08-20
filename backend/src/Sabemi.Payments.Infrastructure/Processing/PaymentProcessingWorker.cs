using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.Infrastructure.Processing;

/// <summary>
/// Consome o sinal em memória e processa os eventos com paralelismo limitado.
/// Nenhuma exceção pode escapar daqui, senão o worker morre e a aplicação para de processar.
/// </summary>
public sealed class PaymentProcessingWorker(
    ChannelPaymentEventQueue queue,
    PaymentEventProcessor processor,
    IOptions<ProcessingOptions> options,
    ILogger<PaymentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Value.MaxDegreeOfParallelism,
            CancellationToken = stoppingToken
        };

        logger.LogInformation(
            "Processamento de pagamentos iniciado com paralelismo {Parallelism}.",
            parallelOptions.MaxDegreeOfParallelism);

        try
        {
            await Parallel.ForEachAsync(
                queue.Reader.ReadAllAsync(stoppingToken),
                parallelOptions,
                async (eventId, cancellationToken) =>
                {
                    try
                    {
                        await processor.ProcessAsync(eventId, cancellationToken);
                    }
                    catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogError(
                            exception,
                            "Erro não tratado ao processar o evento {EventId}.",
                            eventId);
                    }
                });
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Processamento de pagamentos encerrado.");
        }
    }
}
