using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record TransactionDto(
    Guid Id,
    Guid? ContractId,
    Guid? MilestoneId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime CreatedAt)
{
    public static TransactionDto FromEntity(TransactionEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.MilestoneId,
            entity.Type.ToString(),
            entity.Amount,
            entity.BalanceAfter,
            entity.Description,
            entity.CreatedAt);
}