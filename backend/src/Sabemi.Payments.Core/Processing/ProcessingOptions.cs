namespace Sabemi.Payments.Core.Processing;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    /// <summary>
    /// Simula o custo da regra de negócio pesada exigida pelo desafio. É configurável para que a
    /// suíte de testes não pague dois segundos por evento.
    /// </summary>
    public TimeSpan SimulatedWorkDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Tentativas antes de o evento ir para o estado terminal de falha.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Base do backoff exponencial entre tentativas.</summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Eventos processados em paralelo por instância.</summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>Intervalo da varredura que recupera eventos perdidos ou vencidos.</summary>
    public TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Tempo após o qual um evento reservado é considerado abandonado.</summary>
    public TimeSpan StuckTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Máximo de eventos reenfileirados por varredura.</summary>
    public int RecoveryBatchSize { get; set; } = 100;
}
