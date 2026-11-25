using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Features.Payments.GetTransactionHistory;

public class GetTransactionHistoryRequest : ListRequest
{
    public string? Type { get; set; }
}