using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Milestones.ApproveMilestone;

public class ApproveMilestoneValidator : AbstractValidator<ApproveMilestoneRequest>
{
    public ApproveMilestoneValidator()
    {
        RuleFor(x => x.MilestoneId).MustBeValidGuid();
    }
}