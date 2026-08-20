using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Contracts;

/// <summary>Representação de um evento para o painel.</summary>
public sealed record PaymentEventDto(
    Guid Id,
    string TransactionId,
    string? ContractId,
    decimal? Amount,
    DateTimeOffset? PaymentDate,
    string? PaymentStatus,
    EventProcessingStatus ProcessingStatus,
    PaymentView View,
    string? ErrorMessage,
    short Attempts,
    bool HasPayloadDivergence,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ProcessedAt)
{
    public static PaymentEventDto From(WebhookEventLog log) => new(
        log.Id,
        log.TransactionId,
        log.ContractId,
        log.Amount,
        log.PaymentDate,
        log.PaymentStatus,
        log.Status,
        PaymentViews.From(log.Status, log.PaymentStatus),
        log.ErrorMessage,
        log.Attempts,
        log.HasPayloadDivergence,
        log.ReceivedAt,
        log.ProcessedAt);
}
