using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Admin.SearchUsers;

public class SearchUsersResponse(
    int requestedPageNumber,
    int requestedPageSize,
    int totalCount,
    int returnedCount,
    List<UserDto> items)
{
    public int RequestedPageNumber { get; init; } = requestedPageNumber;
    public int RequestedPageSize { get; init; } = requestedPageSize;

    public int TotalCount { get; init; } = totalCount;
    public int ReturnedCount { get; init; } = returnedCount;

    public List<UserDto> Items { get; set; } = items;
}