using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Milestones.GetMilestonesForContract;

public class GetMilestonesForContractValidator : AbstractValidator<GetMilestonesForContractRequest>
{
    public GetMilestonesForContractValidator()
    {
        RuleFor(x => x.ContractId).MustBeValidGuid();
    }
}