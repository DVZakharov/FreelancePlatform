using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Milestones.FundMilestone;

public class FundMilestoneValidator : AbstractValidator<FundMilestoneRequest>
{
    public FundMilestoneValidator()
    {
        RuleFor(x => x.MilestoneId).MustBeValidGuid();
    }
}