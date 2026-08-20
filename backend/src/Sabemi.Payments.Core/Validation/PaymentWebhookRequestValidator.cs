using FluentValidation;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Validation;

/// <summary>
/// Regras de aceitação do payload. As mensagens são escritas em português porque são
/// exibidas diretamente no painel administrativo quando um evento é rejeitado.
/// </summary>
public sealed class PaymentWebhookRequestValidator : AbstractValidator<PaymentWebhookRequest>
{
    /// <summary>Folga para diferença de relógio entre o parceiro e o servidor.</summary>
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public PaymentWebhookRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.TransactionId)
            .NotEmpty().WithMessage("O campo id_transacao é obrigatório.")
            .MaximumLength(100).WithMessage("O campo id_transacao deve ter no máximo 100 caracteres.");

        RuleFor(request => request.ContractId)
            .NotEmpty().WithMessage("O campo id_contrato é obrigatório.")
            .MaximumLength(50).WithMessage("O campo id_contrato deve ter no máximo 50 caracteres.");

        RuleFor(request => request.Amount)
            .NotNull().WithMessage("O campo valor é obrigatório.")
            .GreaterThan(0).WithMessage("O campo valor deve ser maior que zero.")
            .Must(HasAtMostTwoDecimals).WithMessage("O campo valor deve ter no máximo duas casas decimais.")
            .When(request => request.Amount is not null, ApplyConditionTo.CurrentValidator);

        RuleFor(request => request.PaymentDate)
            .NotNull().WithMessage("O campo data_pagamento é obrigatório.")
            .Must(date => date!.Value <= timeProvider.GetUtcNow() + FutureTolerance)
                .WithMessage("O campo data_pagamento não pode estar no futuro.")
            .When(request => request.PaymentDate is not null, ApplyConditionTo.CurrentValidator);

        RuleFor(request => request.Status)
            .NotEmpty().WithMessage("O campo status é obrigatório.")
            .Must(status => PaymentOutcomes.TryParse(status, out _))
                .WithMessage("O campo status deve ser 'sucesso' ou 'erro'.")
            .When(request => !string.IsNullOrWhiteSpace(request.Status), ApplyConditionTo.CurrentValidator);
    }

    /// <summary>
    /// Rejeita precisão maior que a monetária em vez de truncar em silêncio.
    /// </summary>
    private static bool HasAtMostTwoDecimals(decimal? amount) =>
        amount is null || decimal.Round(amount.Value, 2) == amount.Value;
}
