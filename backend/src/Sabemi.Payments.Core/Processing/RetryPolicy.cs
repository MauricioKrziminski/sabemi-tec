namespace Sabemi.Payments.Core.Processing;

public static class RetryPolicy
{
    private const int Factor = 4;

    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    public static TimeSpan DelayFor(int attempt, TimeSpan initialDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        var multiplier = Math.Pow(Factor, attempt - 1);
        var delay = initialDelay * multiplier;

        return delay > MaxDelay ? MaxDelay : delay;
    }

    public static bool IsExhausted(int attempt, int maxAttempts) => attempt >= maxAttempts;
}
