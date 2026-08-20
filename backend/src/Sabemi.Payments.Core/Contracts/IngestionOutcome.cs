using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Contracts;

/// <summary>Resultado do recebimento de um webhook, traduzido pelo endpoint em status HTTP.</summary>
public abstract record IngestionOutcome
{
    /// <summary>O corpo não é um objeto JSON, então não há o que registrar de forma estruturada.</summary>
    public sealed record Unparseable(string Message) : IngestionOutcome;

    /// <summary>Evento aceito e enfileirado para processamento em background.</summary>
    public sealed record Accepted(Guid EventId) : IngestionOutcome;

    /// <summary>Evento registrado, porém reprovado na validação. Aparece no painel com alerta.</summary>
    public sealed record Rejected(Guid EventId, IReadOnlyList<string> Errors) : IngestionOutcome;

    /// <summary>Reenvio de um id de transação já conhecido, respondido de forma idempotente.</summary>
    public sealed record Duplicate(Guid EventId, EventProcessingStatus Status, bool PayloadDiverges) : IngestionOutcome;
}
