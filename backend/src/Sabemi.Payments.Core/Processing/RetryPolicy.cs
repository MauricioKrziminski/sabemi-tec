namespace Sabemi.Payments.Core.Processing;

/// <summary>
/// Backoff exponencial com teto. Sem estado terminal, um evento envenenado giraria para sempre,
/// então a política também decide quando parar de tentar.
/// </summary>
public static class RetryPolicy
{
    private const int Factor = 4;

    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Espera antes da próxima tentativa. Com a base de um segundo: 1s, 4s, 16s, 64s.
    /// </summary>
    /// <param name="attempt">Número da tentativa que acabou de falhar, começando em 1.</param>
    public static TimeSpan DelayFor(int attempt, TimeSpan initialDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        var multiplier = Math.Pow(Factor, attempt - 1);
        var delay = initialDelay * multiplier;

        return delay > MaxDelay ? MaxDelay : delay;
    }

    public static bool IsExhausted(int attempt, int maxAttempts) => attempt >= maxAttempts;
}
