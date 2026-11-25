using FluentValidation;

namespace FreelancePlatform.Application.Features.Disputes.OpenDispute;

public class OpenDisputeValidator : AbstractValidator<OpenDisputeRequest>
{
    public OpenDisputeValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(DisputeTextResponses.ReasonNotNull)
            .MinimumLength(20).WithMessage(DisputeTextResponses.ReasonMinimumSizeError)
            .MaximumLength(2000).WithMessage(DisputeTextResponses.ReasonMaximumSizeError);
    }
}