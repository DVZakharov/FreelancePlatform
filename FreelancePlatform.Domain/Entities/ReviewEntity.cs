using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class ReviewEntity : BaseEntity
{
    public Guid ContractId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid TargetId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public ContractEntity Contract { get; set; } = null!;
}