using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Contracts.CompleteContract;

public class CompleteContractValidator : AbstractValidator<CompleteContractRequest>
{
    public CompleteContractValidator()
    {
        RuleFor(x => x.ContactId).MustBeValidGuid();
    }
}