using System.Text.Json.Nodes;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Contracts;

public sealed record PaymentQuery(PaymentView? View, string? Search, int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static PaymentQuery Create(PaymentView? view, string? search, int? page, int? pageSize) => new(
        view,
        string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
        Math.Max(page ?? 1, 1),
        Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record PaymentEventDetailsDto(
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
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? ProcessingStartedAt,
    string? CorrelationId,
    JsonNode? Payload,
    JsonNode? Headers,
    ContractStatusDto? Contract);

public sealed record ContractStatusDto(
    string ContractId,
    string LastStatus,
    string LastTransactionId,
    DateTimeOffset LastPaymentDate,
    decimal TotalPaid,
    int PaymentCount,
    DateTimeOffset UpdatedAt)
{
    public static ContractStatusDto From(ContractStatus contract) => new(
        contract.ContractId,
        contract.LastStatus,
        contract.LastTransactionId,
        contract.LastPaymentDate,
        contract.TotalPaid,
        contract.PaymentCount,
        contract.UpdatedAt);
}

public sealed record MetricsDto(
    int TotalEvents,
    int Processed,
    int Failures,
    int InProgress,
    decimal TotalSettled,
    int Contracts,
    IReadOnlyList<MetricsBucketDto> Series);

public sealed record MetricsBucketDto(DateTimeOffset Minute, int Total, int Failures);
