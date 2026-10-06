using FluentValidation;

namespace FreelancePlatform.Application.Features.Milestones.CreateMilestone;

public class CreateMilestoneValidator : AbstractValidator<CreateMilestoneRequest>
{
    public CreateMilestoneValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(MilestoneTextResponses.TitleEmpty)
            .MaximumLength(200).WithMessage(MilestoneTextResponses.TitleMaxSizeError);
        
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(MilestoneTextResponses.PriceBelowZeroError);
    }
}