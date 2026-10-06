using FluentValidation;

namespace FreelancePlatform.Application.Features.Admin.BootstrapPromote;

public class BootstrapPromoteValidator : AbstractValidator<BootstrapPromoteRequest>
{
    public BootstrapPromoteValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AdminTextResponses.EmailNotEmpty)
            .EmailAddress().WithMessage(AdminTextResponses.EmailNotValid);
        RuleFor(x => x.SetupSecret).NotEmpty();
    }
}