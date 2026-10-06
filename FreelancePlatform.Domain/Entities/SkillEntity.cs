using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class SkillEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameNormalized { get; set; } = null!;
    public ICollection<FreelancerProfileEntity> FreelancerProfiles { get; set; } = [];
    public ICollection<ProjectEntity> Projects { get; set; } = [];
}