using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Milestones.SubmitMilestone;

public class SubmitMilestoneValidator : AbstractValidator<SubmitMilestoneRequest>
{
    public SubmitMilestoneValidator()
    {
        RuleFor(x => x.MilestoneId).MustBeValidGuid();
    }
}