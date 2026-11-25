using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class MessageEntity : BaseEntity
{
    public Guid ContractId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;

    public ContractEntity Contract { get; set; } = null!;
    public UserEntity Sender { get; set; } = null!;
}