using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Projects.CancelProject;

public class CancelProjectValidator : AbstractValidator<CancelProjectRequest>
{
    public CancelProjectValidator()
    {
        RuleFor(x => x.ProjectId).MustBeValidGuid();
    }
}