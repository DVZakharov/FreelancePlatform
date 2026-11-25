using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Projects.SearchProjects;

public class SearchProjectsResponse
{
    public int RequestedPageNumber { get; init; }
    public int RequestedPageSize { get; init; }

    public int TotalCount { get; init; }
    public int ReturnedCount { get; init; }

    public List<ProjectDto> Items { get; set; } = [];

    public SearchProjectsResponse(int requestedPageNumber, int requestedPageSize, int totalCount, int returnedCount, List<ProjectDto> items)
    {
        RequestedPageNumber = requestedPageNumber;
        RequestedPageSize = requestedPageSize;
        TotalCount = totalCount;
        ReturnedCount = returnedCount;
        Items = items;
    }
}