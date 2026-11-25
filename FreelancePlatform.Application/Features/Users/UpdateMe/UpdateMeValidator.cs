using FluentValidation;

namespace FreelancePlatform.Application.Features.Users.UpdateMe;

public class UpdateMeValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}