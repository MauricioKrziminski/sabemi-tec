using Microsoft.Extensions.Time.Testing;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Validation;

namespace Sabemi.Payments.UnitTests.Validation;

public sealed class PaymentWebhookRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Payload_completo_e_aceito()
    {
        var result = Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("sucesso")]
    [InlineData("erro")]
    [InlineData("SUCESSO")]
    [InlineData(" erro ")]
    public void Status_conhecido_e_aceito_sem_diferenciar_caixa(string status)
    {
        var result = Validate(Valid() with { Status = status });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Id_transacao_vazio_e_recusado()
    {
        var result = Validate(Valid() with { TransactionId = "   " });

        Assert.Contains("O campo id_transacao é obrigatório.", Messages(result));
    }

    [Fact]
    public void Id_contrato_ausente_e_recusado()
    {
        var result = Validate(Valid() with { ContractId = null });

        Assert.Contains("O campo id_contrato é obrigatório.", Messages(result));
    }

    [Fact]
    public void Valor_ausente_e_recusado()
    {
        var result = Validate(Valid() with { Amount = null });

        Assert.Contains("O campo valor é obrigatório.", Messages(result));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.5)]
    public void Valor_nao_positivo_e_recusado(decimal amount)
    {
        var result = Validate(Valid() with { Amount = amount });

        Assert.Contains("O campo valor deve ser maior que zero.", Messages(result));
    }

    [Fact]
    public void Valor_com_mais_de_duas_casas_decimais_e_recusado()
    {
        var result = Validate(Valid() with { Amount = 10.123m });

        Assert.Contains("O campo valor deve ter no máximo duas casas decimais.", Messages(result));
    }

    [Fact]
    public void Data_de_pagamento_no_futuro_e_recusada()
    {
        var result = Validate(Valid() with { PaymentDate = Now.AddHours(1) });

        Assert.Contains("O campo data_pagamento não pode estar no futuro.", Messages(result));
    }

    [Fact]
    public void Data_de_pagamento_dentro_da_tolerancia_de_relogio_e_aceita()
    {
        var result = Validate(Valid() with { PaymentDate = Now.AddMinutes(2) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Status_desconhecido_e_recusado()
    {
        var result = Validate(Valid() with { Status = "pago" });

        Assert.Contains("O campo status deve ser 'sucesso' ou 'erro'.", Messages(result));
    }

    [Fact]
    public void Payload_vazio_devolve_uma_mensagem_por_campo()
    {
        var result = Validate(new PaymentWebhookRequest());

        Assert.Equal(
            [
                "O campo id_transacao é obrigatório.",
                "O campo id_contrato é obrigatório.",
                "O campo valor é obrigatório.",
                "O campo data_pagamento é obrigatório.",
                "O campo status é obrigatório."
            ],
            Messages(result).ToArray());
    }

    private static PaymentWebhookRequest Valid() => new()
    {
        TransactionId = "TRX-8842",
        ContractId = "CT-1029",
        Amount = 1240.00m,
        PaymentDate = Now.AddDays(-1),
        Status = "sucesso"
    };

    private static FluentValidation.Results.ValidationResult Validate(PaymentWebhookRequest request) =>
        new PaymentWebhookRequestValidator(new FakeTimeProvider(Now)).Validate(request);

    private static IEnumerable<string> Messages(FluentValidation.Results.ValidationResult result) =>
        result.Errors.Select(error => error.ErrorMessage);
}
