using Sabemi.Payments.Core.Contracts;

namespace Sabemi.Payments.Core.Processing;

public interface IPaymentEventNotifier
{
    Task EventReceivedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default);

    Task EventUpdatedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default);
}
