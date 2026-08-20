using FluentValidation;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Core.Validation;

public sealed class PaymentWebhookRequestValidator : AbstractValidator<PaymentWebhookRequest>
{
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
            .NotNull().WithMessage("O campo valor é obrigatório.");

        When(request => request.Amount is not null, () =>
            RuleFor(request => request.Amount)
                .GreaterThan(0).WithMessage("O campo valor deve ser maior que zero.")
                .Must(HasAtMostTwoDecimals)
                    .WithMessage("O campo valor deve ter no máximo duas casas decimais."));

        RuleFor(request => request.PaymentDate)
            .NotNull().WithMessage("O campo data_pagamento é obrigatório.");

        When(request => request.PaymentDate is not null, () =>
            RuleFor(request => request.PaymentDate)
                .Must(date => date!.Value <= timeProvider.GetUtcNow() + FutureTolerance)
                    .WithMessage("O campo data_pagamento não pode estar no futuro."));

        RuleFor(request => request.Status)
            .NotEmpty().WithMessage("O campo status é obrigatório.");

        When(request => !string.IsNullOrWhiteSpace(request.Status), () =>
            RuleFor(request => request.Status)
                .Must(status => PaymentOutcomes.TryParse(status, out _))
                    .WithMessage("O campo status deve ser 'sucesso' ou 'erro'."));
    }

    private static bool HasAtMostTwoDecimals(decimal? amount) =>
        amount is null || decimal.Round(amount.Value, 2) == amount.Value;
}
