using Microsoft.AspNetCore.SignalR;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.Api.RealTime;

/// <summary>Publica as mudanças de estado dos eventos para os painéis conectados.</summary>
internal sealed class SignalRPaymentEventNotifier(IHubContext<PaymentsHub> hub) : IPaymentEventNotifier
{
    public Task EventReceivedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(PaymentsHub.PaymentReceived, payment, cancellationToken);

    public Task EventUpdatedAsync(PaymentEventDto payment, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(PaymentsHub.PaymentUpdated, payment, cancellationToken);
}
