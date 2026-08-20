using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.UnitTests.Contracts;

public sealed class PaymentViewsTests
{
    [Theory]
    [InlineData(EventProcessingStatus.Processed, "sucesso", PaymentView.Success)]
    [InlineData(EventProcessingStatus.Processed, "erro", PaymentView.Error)]
    [InlineData(EventProcessingStatus.Invalid, null, PaymentView.Error)]
    [InlineData(EventProcessingStatus.Failed, "sucesso", PaymentView.Error)]
    [InlineData(EventProcessingStatus.PermanentlyFailed, "sucesso", PaymentView.Error)]
    [InlineData(EventProcessingStatus.Pending, "sucesso", PaymentView.Pending)]
    [InlineData(EventProcessingStatus.Processing, "sucesso", PaymentView.Processing)]
    public void Visao_combina_ciclo_de_vida_e_resultado_do_pagamento(
        EventProcessingStatus status,
        string? paymentStatus,
        PaymentView expected)
    {
        Assert.Equal(expected, PaymentViews.From(status, paymentStatus));
    }

    [Fact]
    public void Pagamento_processado_com_status_desconhecido_conta_como_erro()
    {
        Assert.Equal(PaymentView.Error, PaymentViews.From(EventProcessingStatus.Processed, "estornado"));
    }

    [Theory]
    [InlineData("sucesso", PaymentOutcome.Success)]
    [InlineData("ERRO", PaymentOutcome.Error)]
    public void Resultado_do_pagamento_e_lido_do_portugues(string value, PaymentOutcome expected)
    {
        Assert.True(PaymentOutcomes.TryParse(value, out var outcome));
        Assert.Equal(expected, outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pago")]
    public void Resultado_desconhecido_nao_e_convertido(string? value)
    {
        Assert.False(PaymentOutcomes.TryParse(value, out _));
    }
}
