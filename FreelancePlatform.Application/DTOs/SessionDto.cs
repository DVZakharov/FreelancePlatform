using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record SessionDto(
    Guid Id,
    string? UserAgent,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    DateTime ExpiresAt)
{
    public static SessionDto FromEntity(RefreshTokenEntity entity) =>
        new(entity.Id, entity.UserAgent, entity.IpAddress, entity.CreatedAt, entity.LastUsedAt, entity.ExpiresAt);
}