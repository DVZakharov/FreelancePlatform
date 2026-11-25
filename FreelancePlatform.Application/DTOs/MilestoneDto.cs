using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record MilestoneDto(
    Guid Id,
    Guid ContractId,
    string Title,
    decimal Amount,
    string Status,
    bool IsFunded,
    DateTime? DueDate,
    DateTime CreatedAt)
{
    public static MilestoneDto FromEntity(MilestoneEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.Title,
            entity.Amount,
            entity.Status.ToString(),
            entity.Status is Domain.Enums.MilestoneStatus.Funded
                or Domain.Enums.MilestoneStatus.Submitted
                or Domain.Enums.MilestoneStatus.Completed,
            entity.DueDate,
            entity.CreatedAt);
}