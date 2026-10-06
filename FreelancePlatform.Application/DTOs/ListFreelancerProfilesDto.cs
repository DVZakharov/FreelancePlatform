namespace FreelancePlatform.Application.DTOs;

public sealed class ListFreelancerProfilesDto
{
    public int RequestedPageNumber { get; init; }
    public int RequestedPageSize { get; init; }

    public int TotalCount { get; init; }
    public int ReturnedCount { get; init; }

    public List<FreelancerProfileDto> Categories { get; init; } = [];
}
