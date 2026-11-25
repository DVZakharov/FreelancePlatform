using FluentValidation;

namespace FreelancePlatform.Application.Features.Milestones.RejectMilestone;

public class RejectMilestoneValidator : AbstractValidator<RejectMilestoneRequest>
{
    public RejectMilestoneValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(MilestoneTextResponses.ReasonEmpty)
            .MaximumLength(1000).WithMessage(MilestoneTextResponses.ReasonMaximumSizeError);
    }
}