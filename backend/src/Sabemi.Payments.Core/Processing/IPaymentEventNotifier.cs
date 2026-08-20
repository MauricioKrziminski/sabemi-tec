using Sabemi.Payments.Core.Contracts;

namespace Sabemi.Payments.Core.Processing;

/// <summary>Notifica o painel administrativo sobre mudanças em um evento.</summary>
public interface IPaymentEventNotifier
{
    Task EventReceivedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default);

    Task EventUpdatedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default);
}
