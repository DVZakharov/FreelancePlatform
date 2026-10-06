using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Skills.GetSkillById;

public class GetSkillByIdValidator : AbstractValidator<GetSkillByIdRequest>
{
    public GetSkillByIdValidator()
    {
        RuleFor(x => x.Id).MustBeValidGuid();
    }
}