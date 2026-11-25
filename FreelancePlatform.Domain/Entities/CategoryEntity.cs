using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class CategoryEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<ProjectEntity> Projects { get; set; } = [];
}