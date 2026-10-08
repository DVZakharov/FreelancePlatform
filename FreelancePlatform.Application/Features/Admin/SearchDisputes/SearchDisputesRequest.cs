using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Features.Admin.SearchDisputes;

public class SearchDisputesRequest : ListRequest
{
    public string? Status { get; set; }
}