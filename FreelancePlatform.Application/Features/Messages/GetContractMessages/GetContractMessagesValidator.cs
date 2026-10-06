using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Messages.GetContractMessages;

public class GetContractMessagesValidator : AbstractValidator<GetContractMessagesRequest>
{
    public GetContractMessagesValidator()
    {
        Include(new ListRequestValidator<GetContractMessagesRequest>());
    }
}