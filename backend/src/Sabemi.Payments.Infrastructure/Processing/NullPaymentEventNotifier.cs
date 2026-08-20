using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.Infrastructure.Processing;

/// <summary>
/// Implementação padrão para cenários sem painel conectado, como os testes.
/// A API substitui esta implementação pela que publica no hub SignalR.
/// </summary>
public sealed class NullPaymentEventNotifier : IPaymentEventNotifier
{
    public Task EventReceivedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EventUpdatedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
