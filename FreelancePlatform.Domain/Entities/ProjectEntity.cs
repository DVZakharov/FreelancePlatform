using FreelancePlatform.Domain.Common;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Domain.Entities;

public class ProjectEntity : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BudgetType BudgetType { get; set; }
    public decimal BudgetMin { get; set; }
    public decimal BudgetMax { get; set; }
    public DateTime Deadline { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public Guid? SelectedProposalId { get; set; }
    public DateTime? PublishedAt { get; set; }

    public UserEntity Client { get; set; } = null!;
    public CategoryEntity Category { get; set; } = null!;
    public ICollection<SkillEntity> RequiredSkills { get; set; } = [];
    public ICollection<ProposalEntity> Proposals { get; set; } = [];
    public ContractEntity? Contract { get; set; }
}