namespace Sabemi.Payments.Core.Domain;

/// <summary>
/// Ciclo de vida de um evento recebido do banco parceiro.
/// </summary>
public enum EventProcessingStatus
{
    /// <summary>Aceito e aguardando processamento em background.</summary>
    Pending,

    /// <summary>Reservado por um worker e em processamento.</summary>
    Processing,

    /// <summary>Regra de negócio aplicada com sucesso.</summary>
    Processed,

    /// <summary>Rejeitado na validação do payload, não será processado.</summary>
    Invalid,

    /// <summary>Falhou no processamento e será tentado novamente.</summary>
    Failed,

    /// <summary>Esgotou as tentativas de processamento, estado terminal.</summary>
    PermanentlyFailed
}
