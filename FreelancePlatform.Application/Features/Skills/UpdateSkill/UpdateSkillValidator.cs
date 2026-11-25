using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Skills.UpdateSkill;

public class UpdateSkillValidator : AbstractValidator<UpdateSkillRequest>
{
    public UpdateSkillValidator()
    {
        RuleFor(x => x.Name).MustBeValidName();
    }
}
