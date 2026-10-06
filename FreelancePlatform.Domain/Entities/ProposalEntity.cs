using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class ProposalEntity : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid FreelancerId { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public string CoverLetter { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    public ProjectEntity Project { get; set; } = null!;
    public UserEntity Freelancer { get; set; } = null!;
}