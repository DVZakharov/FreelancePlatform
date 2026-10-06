using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    List<string> Roles,
    decimal Balance,
    bool IsFreelancer,
    DateTime CreatedAt)
{
    public static UserDto FromEntity(UserEntity entity, bool isFreelancer) =>
        new(
            entity.Id,
            entity.Email,
            entity.FullName,
            Enum.GetValues<Domain.Enums.UserRole>()
                .Where(r => r != Domain.Enums.UserRole.None && entity.Role.HasFlag(r))
                .Select(r => r.ToString())
                .ToList(),
            entity.Balance,
            isFreelancer,
            entity.CreatedAt);
}