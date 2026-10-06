using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record ProposalDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid FreelancerId,
    string FreelancerName,
    decimal Price,
    int EstimatedDays,
    string CoverLetter,
    string Status,
    DateTime CreatedAt)
{
    public static ProposalDto FromEntity(ProposalEntity entity) =>
        new(
            entity.Id,
            entity.ProjectId,
            entity.Project.Title,
            entity.FreelancerId,
            entity.Freelancer.FullName,
            entity.Price,
            entity.EstimatedDays,
            entity.CoverLetter,
            entity.Status.ToString(),
            entity.CreatedAt);
}