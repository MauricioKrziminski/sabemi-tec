using System.Text.Json.Serialization;

namespace Sabemi.Payments.Core.Contracts;

/// <summary>
/// Payload enviado pelo banco parceiro. Os nomes seguem exatamente o contrato acordado,
/// em português, enquanto o restante do código permanece em inglês.
///
/// Todos os campos são opcionais na desserialização de propósito: assim um campo ausente vira
/// uma mensagem de validação legível no painel, e não uma falha de leitura do JSON.
/// </summary>
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
