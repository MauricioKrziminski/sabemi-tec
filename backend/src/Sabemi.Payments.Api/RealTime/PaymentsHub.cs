using Microsoft.AspNetCore.SignalR;

namespace Sabemi.Payments.Api.RealTime;

/// <summary>
/// Canal de tempo real do painel. O fluxo é unidirecional, do servidor para os clientes,
/// então o hub não expõe métodos de entrada.
/// </summary>
public sealed class PaymentsHub : Hub
{
    public const string Route = "/hubs/payments";

    public const string PaymentReceived = "paymentReceived";

    public const string PaymentUpdated = "paymentUpdated";
}
