using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class FreelancerProfileEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public string Bio { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }

    public UserEntity User { get; set; } = null!;
    public ICollection<SkillEntity> Skills { get; set; } = [];
}