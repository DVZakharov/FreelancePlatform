using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.SearchProfiles;

public class SearchProfilesValidator : AbstractValidator<SearchProfilesRequest>
{
    public SearchProfilesValidator()
    {
        Include(new ListRequestValidator<SearchProfilesRequest>());

        RuleFor(x => x.MinRating)
           .InclusiveBetween(0, 5).WithMessage(ValidatorsTextResponses.InvalidRating)
           .When(x => x.MinRating.HasValue);

        RuleFor(x => x.SkillIds).MustAllBeValidGuids();
    }
}
