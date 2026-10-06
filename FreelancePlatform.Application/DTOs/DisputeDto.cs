using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record DisputeDto(
    Guid Id,
    Guid ContractId,
    Guid OpenedById,
    string Reason,
    string Status,
    string? Resolution,
    Guid? ResolvedById,
    DateTime? ResolvedAt,
    DateTime CreatedAt)
{
    public static DisputeDto FromEntity(DisputeEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.OpenedById,
            entity.Reason,
            entity.Status.ToString(),
            entity.Resolution,
            entity.ResolvedById,
            entity.ResolvedAt,
            entity.CreatedAt);
}