using FluentValidation;
using System.Text.RegularExpressions;

namespace FreelancePlatform.Application.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage(AuthTextResponses.InvalidEmailEmpty)
            .EmailAddress().WithMessage(AuthTextResponses.InvalidEmail)
            .MaximumLength(255);
        RuleFor(p => p.Password)
           .NotEmpty().WithMessage(AuthTextResponses.InvalidPasswordEmpty)
           .MinimumLength(8).WithMessage(AuthTextResponses.InvalidPassword8)
           .MaximumLength(100).WithMessage(AuthTextResponses.InvalidPassword100)
           .Must(ContainUppercase)
               .WithMessage(AuthTextResponses.InvalidPasswordUppercase)
           .Must(ContainLowercase)
               .WithMessage(AuthTextResponses.InvalidPasswordLowercase)
           .Must(ContainDigit)
               .WithMessage(AuthTextResponses.InvalidPasswordDigit)
           .Must(ContainSpecialChar)
               .WithMessage(AuthTextResponses.InvalidPasswordSpecialChar);

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(AuthTextResponses.InvalidFullName);
    }

    private bool ContainUppercase(string password) =>
       password.Any(char.IsUpper);

    private bool ContainLowercase(string password) =>
        password.Any(char.IsLower);

    private bool ContainDigit(string password) =>
        password.Any(char.IsDigit);

    private bool ContainSpecialChar(string password) =>
        Regex.IsMatch(password, "[!@#$%^&*(),.?\\\":{}|<>_\\-+=/\\[\\]\\\\;']");
}