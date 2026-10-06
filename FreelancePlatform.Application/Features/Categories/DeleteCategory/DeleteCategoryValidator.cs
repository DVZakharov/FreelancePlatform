using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Categories.DeleteCategory;

public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id).MustBeValidGuid();
    }
}