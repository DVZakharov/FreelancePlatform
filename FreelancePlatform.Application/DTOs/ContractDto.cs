using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record ContractDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid ClientId,
    string ClientName,
    Guid FreelancerId,
    string FreelancerName,
    decimal AgreedPrice,
    DateTime AgreedDeadline,
    string Status,
    DateTime CreatedAt)
{
    public static ContractDto FromEntity(ContractEntity entity) =>
        new(
            entity.Id,
            entity.ProjectId,
            entity.Project.Title,
            entity.ClientId,
            entity.Client.FullName,
            entity.FreelancerId,
            entity.Freelancer.FullName,
            entity.AgreedPrice,
            entity.AgreedDeadline,
            entity.Status.ToString(),
            entity.CreatedAt);
}