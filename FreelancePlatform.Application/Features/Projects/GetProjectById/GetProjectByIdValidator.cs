using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Projects.GetProjectById;

public class GetProjectByIdValidator : AbstractValidator<GetProjectByIdRequest>
{
    public GetProjectByIdValidator()
    {
        RuleFor(x => x.ProjectId).MustBeValidGuid();
    }
}