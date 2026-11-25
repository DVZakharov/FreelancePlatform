using FluentValidation;

namespace FreelancePlatform.Application.Features.Payments.TopUpBalance;

public class TopUpBalanceValidator : AbstractValidator<TopUpBalanceRequest>
{
    public TopUpBalanceValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(PaymentTextResponses.AmountBelowZero)
            .LessThanOrEqualTo(1_000_000).WithMessage(PaymentTextResponses.AmountGreaterMillion);
    }
}