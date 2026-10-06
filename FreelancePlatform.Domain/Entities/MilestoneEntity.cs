using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class MilestoneEntity : BaseEntity
{
    public Guid ContractId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public DateTime? DueDate { get; set; }

    public ContractEntity Contract { get; set; } = null!;
    public EscrowHoldEntity? EscrowHold { get; set; }
    public ICollection<TransactionEntity> Transactions { get; set; } = [];
}