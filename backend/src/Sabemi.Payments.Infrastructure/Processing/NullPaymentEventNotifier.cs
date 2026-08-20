using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.Infrastructure.Processing;

public sealed class NullPaymentEventNotifier : IPaymentEventNotifier
{
    public Task EventReceivedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EventUpdatedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
