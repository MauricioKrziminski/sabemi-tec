namespace Sabemi.Payments.Core.Domain;

public enum PaymentOutcome
{
    Success,

    Error
}

public static class PaymentOutcomes
{
    private const string SuccessLabel = "sucesso";
    private const string ErrorLabel = "erro";

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
