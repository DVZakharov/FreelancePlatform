using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class EscrowHoldEntity : BaseEntity
{
    public Guid MilestoneId { get; set; }
    public Guid ContractId { get; set; }
    public decimal Amount { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Held;
    public DateTime? ResolvedAt { get; set; }

    public MilestoneEntity Milestone { get; set; } = null!;
    public ContractEntity Contract { get; set; } = null!;
}