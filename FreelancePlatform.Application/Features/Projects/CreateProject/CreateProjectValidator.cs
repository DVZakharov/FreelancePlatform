using FluentValidation;
using FreelancePlatform.Application.Common.Validators;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Application.Features.Projects.CreateProject;

public class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.CategoryId).MustBeValidGuid();
        
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(ProjectTextResponses.InvalidTitleMinLenght)
            .MaximumLength(200).WithMessage(ProjectTextResponses.InvalidTitleMaxLenght);
        
        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage(ProjectTextResponses.InvalidDescriptionMaxLenght);
        
        RuleFor(x => x.BudgetType).MustBeValidEnum<CreateProjectRequest, BudgetType>();
        
        RuleFor(x => x.BudgetMin).GreaterThanOrEqualTo(0).WithMessage(ProjectTextResponses.InvalidBudgetMin);
        
        RuleFor(x => x.BudgetMax).GreaterThanOrEqualTo(x => x.BudgetMin).WithMessage(ProjectTextResponses.InvalidBudgetMax);
        
        RuleForEach(x => x.SkillIds).MustBeValidGuid();
    }
}