using FluentValidation;
using FreelancePlatform.Application.Common.Validators;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Application.Features.Admin.SearchUsers;

public class SearchUsersValidator : AbstractValidator<SearchUsersRequest>
{
    public SearchUsersValidator()
    {
        Include(new ListRequestValidator<SearchUsersRequest>());

        RuleFor(x => x.Search).MaximumLength(100);

        RuleFor(x => x.Role)
            .Must(r => r is null || (Enum.TryParse<UserRole>(r, true, out var parsed) && parsed != UserRole.None))
            .WithMessage(AdminTextResponses.RoleSelectError);
    }
}