using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Reviews.GetReviewsForContract;

public class GetReviewsForContractValidator : AbstractValidator<GetReviewsForContractRequest>
{
    public GetReviewsForContractValidator()
    {
        RuleFor(r => r.ContractId).MustBeValidGuid();
    }
}