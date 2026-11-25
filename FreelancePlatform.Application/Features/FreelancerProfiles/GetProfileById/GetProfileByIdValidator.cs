using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.GetProfileById;

public class GetProfileByIdValidator : AbstractValidator<GetProfileByIdRequest>
{
    public GetProfileByIdValidator()
    {
        RuleFor(x => x.Id).MustBeValidGuid();
    }
}
