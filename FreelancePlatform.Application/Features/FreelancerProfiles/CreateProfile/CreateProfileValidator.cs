using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.CreateProfile;

public class CreateProfileValidator : AbstractValidator<CreateProfileRequest>
{
    public CreateProfileValidator()
    {
        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage(ProfileTextResponses.BioEmpty)
            .MinimumLength(50).WithMessage(ProfileTextResponses.BioTooShort)
            .MaximumLength(2000).WithMessage(ProfileTextResponses.BioTooLong);

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0).WithMessage(ProfileTextResponses.HourlyRateInvalid);

        RuleFor(x => x.SkillIds)
            .NotEmpty().WithMessage(ProfileTextResponses.SkillIdsEmpty)
            .MustAllBeValidGuids();
    }
}
