namespace Sabemi.Payments.Core.Domain;

/// <summary>
/// Resultado da liquidação informado pelo banco parceiro no campo <c>status</c>.
/// </summary>
public enum PaymentOutcome
{
    /// <summary>Pagamento liquidado, soma no total do contrato.</summary>
    Success,

    /// <summary>Pagamento recusado, não soma no total do contrato.</summary>
    Error
}

public static class PaymentOutcomes
{
    private const string SuccessLabel = "sucesso";
    private const string ErrorLabel = "erro";

    /// <summary>
    /// Converte o valor recebido no payload, que chega em português, para o domínio.
    /// </summary>
    public static bool TryParse(string? value, out PaymentOutcome outcome)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case SuccessLabel:
                outcome = PaymentOutcome.Success;
                return true;
            case ErrorLabel:
                outcome = PaymentOutcome.Error;
                return true;
            default:
                outcome = default;
                return false;
        }
    }

    public static string ToLabel(this PaymentOutcome outcome) =>
        outcome == PaymentOutcome.Success ? SuccessLabel : ErrorLabel;
}
