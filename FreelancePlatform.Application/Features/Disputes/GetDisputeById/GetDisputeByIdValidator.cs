using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Disputes.GetDisputeById;

public class GetDisputeByIdValidator : AbstractValidator<GetDisputeByIdRequest>
{
    public GetDisputeByIdValidator()
    {
        RuleFor(x => x.DisputeId).MustBeValidGuid();
    }    
}