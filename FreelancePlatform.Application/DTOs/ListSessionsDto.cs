namespace FreelancePlatform.Application.DTOs;

public sealed class ListSessionsDto
{
    public int Count { get; set; }

    public List<SessionDto> Sessions { get; set; } = [];
}
