using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Disputes.GetDisputesForContract;

public class GetDisputesForContractValidator : AbstractValidator<GetDisputesForContractRequest>
{
    public GetDisputesForContractValidator()
    {
        RuleFor(d => d.ContractId).MustBeValidGuid();
    }
}