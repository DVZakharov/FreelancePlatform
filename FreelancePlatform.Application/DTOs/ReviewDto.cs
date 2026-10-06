using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record ReviewDto(
    Guid Id,
    Guid ContractId,
    Guid AuthorId,
    string AuthorName,
    Guid TargetId,
    string TargetName,
    int Rating,
    string? Comment,
    DateTime CreatedAt)
{
    public static ReviewDto FromEntity(ReviewEntity entity, string authorName, string targetName) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.AuthorId,
            authorName,
            entity.TargetId,
            targetName,
            entity.Rating,
            entity.Comment,
            entity.CreatedAt);
}