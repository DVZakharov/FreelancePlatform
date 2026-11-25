using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Contracts.GetContractById;

public class GetContractByIdValidator : AbstractValidator<GetContractByIdRequest>
{
    public GetContractByIdValidator()
    {
        RuleFor(x => x.ContractId).MustBeValidGuid();
    }
}