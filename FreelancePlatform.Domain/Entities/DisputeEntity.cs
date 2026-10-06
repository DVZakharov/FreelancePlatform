using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class DisputeEntity : BaseEntity
{
    public Guid ContractId { get; set; }
    public Guid OpenedById { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }
    public Guid? ResolvedById { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ContractEntity Contract { get; set; } = null!;
}