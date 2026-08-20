namespace Sabemi.Payments.Core.Domain;

public sealed class ContractStatus
{
    public required string ContractId { get; init; }

    public required string LastStatus { get; set; }

    public required string LastTransactionId { get; set; }

    public DateTimeOffset LastPaymentDate { get; set; }

    public decimal TotalPaid { get; set; }

    public int PaymentCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
