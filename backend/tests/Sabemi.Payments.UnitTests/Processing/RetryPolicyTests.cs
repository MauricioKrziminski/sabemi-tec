using Sabemi.Payments.Core.Processing;

namespace Sabemi.Payments.UnitTests.Processing;

public sealed class RetryPolicyTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 4)]
    [InlineData(3, 16)]
    [InlineData(4, 64)]
    public void Backoff_cresce_de_forma_exponencial(int attempt, int expectedSeconds)
    {
        var delay = RetryPolicy.DelayFor(attempt, TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void Backoff_respeita_o_teto()
    {
        var delay = RetryPolicy.DelayFor(10, TimeSpan.FromSeconds(1));

        Assert.Equal(RetryPolicy.MaxDelay, delay);
    }

    [Fact]
    public void Tentativa_invalida_e_rejeitada()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicy.DelayFor(0, TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(4, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    public void Estado_terminal_e_alcancado_na_ultima_tentativa(int attempt, int maxAttempts, bool expected)
    {
        Assert.Equal(expected, RetryPolicy.IsExhausted(attempt, maxAttempts));
    }
}
