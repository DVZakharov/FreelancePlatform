using FluentValidation;

namespace FreelancePlatform.Application.Features.Disputes.ResolveDispute;

public class ResolveDisputeValidator : AbstractValidator<ResolveDisputeRequest>
{
    public ResolveDisputeValidator()
    {
        RuleFor(x => x.Decision)
            .Must(d => d is "Client" or "Freelancer")
            .WithMessage(DisputeTextResponses.InvalidDecision);

        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage(DisputeTextResponses.ResolutionEmpty)
            .MaximumLength(2000).WithMessage(DisputeTextResponses.ResolutionMaximumSizeError);
    }
}