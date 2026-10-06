using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record FreelancerProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Bio,
    decimal HourlyRate,
    decimal Rating,
    List<SkillDto> Skills)
{
    public static FreelancerProfileDto FromEntity(FreelancerProfileEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.User.FullName,
            entity.Bio,
            entity.HourlyRate,
            entity.Rating,
            [.. entity.Skills.Select(s => new SkillDto(s.Id, s.Name))]);
}