using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Projects.PublishProject;

public class PublishProjectValidator : AbstractValidator<PublishProjectRequest>
{
    public PublishProjectValidator()
    {
        RuleFor(r => r.ProjectId).MustBeValidGuid();
    }
}