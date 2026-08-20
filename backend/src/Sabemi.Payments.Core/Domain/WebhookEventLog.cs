namespace Sabemi.Payments.Core.Domain;

public sealed class WebhookEventLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string TransactionId { get; init; }

    public string? ContractId { get; set; }

    public decimal? Amount { get; set; }

    public DateTimeOffset? PaymentDate { get; set; }

    public string? PaymentStatus { get; set; }

    public required string Payload { get; init; }

    public required byte[] PayloadHash { get; init; }

    public string? Headers { get; init; }

    public EventProcessingStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public short Attempts { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? CorrelationId { get; init; }

    public bool HasPayloadDivergence { get; set; }

    public bool IsTerminal => Status is EventProcessingStatus.Processed
        or EventProcessingStatus.Invalid
        or EventProcessingStatus.PermanentlyFailed;
}
