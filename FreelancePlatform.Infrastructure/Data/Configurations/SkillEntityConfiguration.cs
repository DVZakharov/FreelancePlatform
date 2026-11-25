using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class SkillEntityConfiguration : IEntityTypeConfiguration<SkillEntity>
{
    public void Configure(EntityTypeBuilder<SkillEntity> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasColumnType("citext").IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(s => s.FreelancerProfiles)
            .WithMany(fp => fp.Skills)
            .UsingEntity(j => j.ToTable("FreelancerProfileSkills"));

        builder.HasMany(s => s.Projects)
            .WithMany(p => p.RequiredSkills)
            .UsingEntity(j => j.ToTable("ProjectSkills"));
    }
}