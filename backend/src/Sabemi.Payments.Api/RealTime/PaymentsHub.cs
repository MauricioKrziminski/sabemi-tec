using Microsoft.AspNetCore.SignalR;

namespace Sabemi.Payments.Api.RealTime;

public sealed class PaymentsHub : Hub
{
    public const string Route = "/hubs/payments";

    public const string PaymentReceived = "paymentReceived";

    public const string PaymentUpdated = "paymentUpdated";
}
