using FluentValidation;

namespace FreelancePlatform.Application.Features.Messages.SendMessage;

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(MessageTextResponses.MessageEmpty)
            .MaximumLength(5000).WithMessage(MessageTextResponses.MessageMaximumSizeError);
    }
}