namespace Sabemi.Payments.Core.Domain;

/// <summary>
/// Log de eventos brutos. Guarda exatamente o que o banco parceiro enviou, mesmo quando o
/// payload é inválido, e é também a fila durável do processamento (padrão transactional inbox).
/// </summary>
public sealed class WebhookEventLog
{
    /// <summary>UUID v7, ordenável no tempo, o que evita fragmentação de índice.</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Chave de idempotência enviada pelo parceiro, com índice único.</summary>
    public required string TransactionId { get; init; }

    public string? ContractId { get; set; }

    public decimal? Amount { get; set; }

    public DateTimeOffset? PaymentDate { get; set; }

    /// <summary>Valor de <c>status</c> exatamente como veio no payload.</summary>
    public string? PaymentStatus { get; set; }

    /// <summary>Corpo original da requisição, armazenado como jsonb.</summary>
    public required string Payload { get; init; }

    /// <summary>SHA-256 do corpo original, usado para detectar reenvio com conteúdo divergente.</summary>
    public required byte[] PayloadHash { get; init; }

    /// <summary>Subconjunto seguro dos headers, sem a assinatura.</summary>
    public string? Headers { get; init; }

    public EventProcessingStatus Status { get; set; }

    /// <summary>Mensagem exibida no painel quando o evento falha na validação ou no processamento.</summary>
    public string? ErrorMessage { get; set; }

    public short Attempts { get; set; }

    /// <summary>Momento a partir do qual o evento pode ser tentado novamente.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>Preenchido enquanto um worker mantém o evento reservado.</summary>
    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? CorrelationId { get; init; }

    /// <summary>Verdadeiro quando um reenvio trouxe o mesmo id de transação com corpo diferente.</summary>
    public bool HasPayloadDivergence { get; set; }

    /// <summary>Estados em que o evento não avança mais sozinho.</summary>
    public bool IsTerminal => Status is EventProcessingStatus.Processed
        or EventProcessingStatus.Invalid
        or EventProcessingStatus.PermanentlyFailed;
}
