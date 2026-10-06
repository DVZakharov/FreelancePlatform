using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Skills.DeleteSkill;

public class DeleteSkillValidator : AbstractValidator<DeleteSkillRequest>
{
    public DeleteSkillValidator()
    {
        RuleFor(x => x.Id).MustBeValidGuid();
    }
}