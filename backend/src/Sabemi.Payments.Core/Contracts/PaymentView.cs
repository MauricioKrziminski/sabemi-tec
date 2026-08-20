using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Contracts;

/// <summary>
/// Visão que o painel usa para filtrar, combinando o ciclo de vida do evento com o
/// resultado informado pelo banco. É o "Sucesso/Erro" pedido no requisito de filtros.
/// </summary>
public enum PaymentView
{
    Success,
    Error,
    Pending,
    Processing
}

public static class PaymentViews
{
    public static PaymentView From(EventProcessingStatus status, string? paymentStatus) => status switch
    {
        EventProcessingStatus.Invalid => PaymentView.Error,
        EventProcessingStatus.Failed => PaymentView.Error,
        EventProcessingStatus.PermanentlyFailed => PaymentView.Error,
        EventProcessingStatus.Processing => PaymentView.Processing,
        EventProcessingStatus.Pending => PaymentView.Pending,
        EventProcessingStatus.Processed => PaymentOutcomes.TryParse(paymentStatus, out var outcome)
            && outcome == PaymentOutcome.Success
                ? PaymentView.Success
                : PaymentView.Error,
        _ => PaymentView.Pending
    };
}
