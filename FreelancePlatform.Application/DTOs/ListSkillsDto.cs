namespace FreelancePlatform.Application.DTOs;

public sealed class ListSkillsDto
{
    public int RequestedPageNumber { get; init; }
    public int RequestedPageSize { get; init; }

    public int TotalCount { get; init; }
    public int ReturnedCount { get; init; }

    public List<SkillDto> Skills { get; init; } = [];
}
