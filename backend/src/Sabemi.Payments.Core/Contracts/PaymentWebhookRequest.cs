using System.Text.Json.Serialization;

namespace Sabemi.Payments.Core.Contracts;

public sealed record PaymentWebhookRequest
{
    [JsonPropertyName("id_transacao")]
    public string? TransactionId { get; init; }

    [JsonPropertyName("id_contrato")]
    public string? ContractId { get; init; }

    [JsonPropertyName("valor")]
    public decimal? Amount { get; init; }

    [JsonPropertyName("data_pagamento")]
    public DateTimeOffset? PaymentDate { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
