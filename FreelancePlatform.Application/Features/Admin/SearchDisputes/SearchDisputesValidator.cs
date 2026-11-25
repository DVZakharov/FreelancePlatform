using FluentValidation;
using FreelancePlatform.Application.Common.Validators;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Application.Features.Admin.SearchDisputes;

public class SearchDisputesValidator : AbstractValidator<SearchDisputesRequest>
{
    public SearchDisputesValidator()
    {
        Include(new ListRequestValidator<SearchDisputesRequest>());

        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<DisputeStatus>(s, true, out _))
            .WithMessage(AdminTextResponses.StatusSelectError);
    }
}