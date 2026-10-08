using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Admin.SearchDisputes;

public class SearchDisputesResponse(
    int requestedPageNumber,
    int requestedPageSize,
    int totalCount,
    int returnedCount,
    List<DisputeDto> items)
{
    public int RequestedPageNumber { get; init; } = requestedPageNumber;
    public int RequestedPageSize { get; init; } = requestedPageSize;

    public int TotalCount { get; init; } = totalCount;
    public int ReturnedCount { get; init; } = returnedCount;

    public List<DisputeDto> Items { get; set; } = items;
}
