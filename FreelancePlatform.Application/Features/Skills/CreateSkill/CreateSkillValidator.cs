using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Skills.CreateSkill;

public class CreateSkillValidator : AbstractValidator<CreateSkillRequest>
{
    public CreateSkillValidator()
    {
        RuleFor(x => x.Name).MustBeValidName();
    }
}