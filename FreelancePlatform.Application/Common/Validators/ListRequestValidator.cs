using FluentValidation;
using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Common.Validators;

public class ListRequestValidator<T> : AbstractValidator<T> where T : ListRequest
{
    public ListRequestValidator()
    {
        RuleFor(x => x.PageNumber).MustBeValidPageNumber();
        RuleFor(x => x.PageSize).MustBeValidPageSize();
    }
}
