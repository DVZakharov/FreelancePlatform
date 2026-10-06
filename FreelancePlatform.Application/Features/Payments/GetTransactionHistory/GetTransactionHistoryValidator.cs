using FluentValidation;
using FreelancePlatform.Application.Common.Validators;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Application.Features.Payments.GetTransactionHistory;

public class GetTransactionHistoryValidator : AbstractValidator<GetTransactionHistoryRequest>
{
    public GetTransactionHistoryValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Type), () =>
        {
            RuleFor(x => x.Type)!
                .MustBeValidEnum<GetTransactionHistoryRequest, TransactionType>();
        });

        Include(new ListRequestValidator<GetTransactionHistoryRequest>());
    }
}