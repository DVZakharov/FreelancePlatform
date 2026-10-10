using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class UserEntity : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public decimal Balance { get; set; }

    public uint Version { get; set; }
    
    public FreelancerProfileEntity? FreelancerProfile { get; set; }
    public ICollection<ProjectEntity> ProjectsAsClient { get; set; } = [];
    public ICollection<ProposalEntity> Proposals { get; set; } = [];
    public ICollection<TransactionEntity> Transactions { get; set; } = [];
    public ICollection<MessageEntity> Messages { get; set; } = [];
}