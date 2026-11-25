using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Projects.SearchProjects;

public class SearchProjectsValidator : AbstractValidator<SearchProjectsRequest>
{
    public SearchProjectsValidator()
    {
        Include(new ListRequestValidator<SearchProjectsRequest>());

        RuleFor(x => x.CategoryId).MustBeValidGuid().When(x => x.CategoryId is not null);
        RuleForEach(x => x.SkillIds).MustBeValidGuid();
    }
}