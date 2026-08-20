using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sabemi.Payments.Core.Security;

namespace Sabemi.Payments.UnitTests.Security;

public sealed class WebhookSignatureValidatorTests
{
    private const string Secret = "segredo-compartilhado";
    private const string Body = """{"id_transacao":"TRX-1","valor":10.5}""";

    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Assinatura_valida_e_aceita()
    {
        var (validator, timestamp) = Build();

        var result = validator.Validate(Body, WebhookSignatureValidator.Compute(Secret, timestamp, Body), timestamp.ToString());

        Assert.True(result.IsValid);
        Assert.Equal(SignatureFailureReason.None, result.Reason);
    }

    [Fact]
    public void Assinatura_sem_prefixo_tambem_e_aceita()
    {
        var (validator, timestamp) = Build();
        var signature = WebhookSignatureValidator.Compute(Secret, timestamp, Body).Replace("sha256=", string.Empty);

        var result = validator.Validate(Body, signature, timestamp.ToString());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Corpo_alterado_em_um_caractere_invalida_a_assinatura()
    {
        var (validator, timestamp) = Build();
        var signature = WebhookSignatureValidator.Compute(Secret, timestamp, Body);

        var result = validator.Validate(Body.Replace("10.5", "10.6"), signature, timestamp.ToString());

        Assert.False(result.IsValid);
        Assert.Equal(SignatureFailureReason.SignatureMismatch, result.Reason);
    }

    [Fact]
    public void Segredo_diferente_invalida_a_assinatura()
    {
        var (validator, timestamp) = Build();

        var result = validator.Validate(Body, WebhookSignatureValidator.Compute("outro-segredo", timestamp, Body), timestamp.ToString());

        Assert.False(result.IsValid);
        Assert.Equal(SignatureFailureReason.SignatureMismatch, result.Reason);
    }

    [Fact]
    public void Assinatura_capturada_nao_pode_ser_reenviada_com_outro_carimbo()
    {
        var (validator, timestamp) = Build();
        var signature = WebhookSignatureValidator.Compute(Secret, timestamp, Body);

        var result = validator.Validate(Body, signature, (timestamp + 60).ToString());

        Assert.False(result.IsValid);
        Assert.Equal(SignatureFailureReason.SignatureMismatch, result.Reason);
    }

    [Fact]
    public void Carimbo_fora_da_janela_e_recusado()
    {
        var (validator, timestamp) = Build();
        var expired = timestamp - (long)TimeSpan.FromMinutes(10).TotalSeconds;

        var result = validator.Validate(Body, WebhookSignatureValidator.Compute(Secret, expired, Body), expired.ToString());

        Assert.False(result.IsValid);
        Assert.Equal(SignatureFailureReason.TimestampOutOfWindow, result.Reason);
    }

    [Fact]
    public void Carimbo_dentro_da_janela_e_aceito_mesmo_com_relogio_adiantado()
    {
        var (validator, timestamp) = Build();
        var ahead = timestamp + (long)TimeSpan.FromMinutes(2).TotalSeconds;

        var result = validator.Validate(Body, WebhookSignatureValidator.Compute(Secret, ahead, Body), ahead.ToString());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, "1787241187", SignatureFailureReason.MissingSignature)]
    [InlineData("", "1787241187", SignatureFailureReason.MissingSignature)]
    [InlineData("sha256=abc", null, SignatureFailureReason.MissingTimestamp)]
    [InlineData("sha256=abc", "agora", SignatureFailureReason.MalformedTimestamp)]
    public void Headers_ausentes_ou_malformados_sao_recusados(
        string? signature,
        string? timestamp,
        SignatureFailureReason expected)
    {
        var (validator, _) = Build();

        var result = validator.Validate(Body, signature, timestamp);

        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Reason);
    }

    [Fact]
    public void Assinatura_que_nao_e_hexadecimal_e_recusada()
    {
        var (validator, timestamp) = Build();

        var result = validator.Validate(Body, "sha256=nao-e-hexadecimal", timestamp.ToString());

        Assert.False(result.IsValid);
        Assert.Equal(SignatureFailureReason.MalformedSignature, result.Reason);
    }

    private static (WebhookSignatureValidator Validator, long Timestamp) Build()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var options = Options.Create(new WebhookSignatureOptions { Secret = Secret });

        return (new WebhookSignatureValidator(options, timeProvider), Now.ToUnixTimeSeconds());
    }
}
