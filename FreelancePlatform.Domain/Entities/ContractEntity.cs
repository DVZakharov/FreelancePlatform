using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class ContractEntity : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid ClientId { get; set; }
    public Guid FreelancerId { get; set; }
    public decimal AgreedPrice { get; set; }
    public DateTime AgreedDeadline { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime? CompletedAt { get; set; }

    public ProjectEntity Project { get; set; } = null!;
    public UserEntity Client { get; set; } = null!;
    public UserEntity Freelancer { get; set; } = null!;
    public ICollection<MilestoneEntity> Milestones { get; set; } = [];
    public ICollection<DisputeEntity> Disputes { get; set; } = [];
    public ICollection<ReviewEntity> Reviews { get; set; } = [];
    public ICollection<MessageEntity> Messages { get; set; } = [];
}