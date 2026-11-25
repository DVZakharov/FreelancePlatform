using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class TransactionEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? MilestoneId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }

    public UserEntity User { get; set; } = null!;
    public MilestoneEntity? Milestone { get; set; }
}