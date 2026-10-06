using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Categories.GetCategoryById;

public class GetCategoryByIdValidator : AbstractValidator<GetCategoryByIdRequest>
{
    public GetCategoryByIdValidator()
    {
        RuleFor(x => x.Id).MustBeValidGuid();
    }
}