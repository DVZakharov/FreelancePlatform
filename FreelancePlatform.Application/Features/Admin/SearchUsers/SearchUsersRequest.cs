using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Features.Admin.SearchUsers;

public class SearchUsersRequest : ListRequest
{
    public string? Search { get; set; }
    public string? Role { get; set; }
}