using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class FreelancerProfileEntityConfiguration : IEntityTypeConfiguration<FreelancerProfileEntity>
{
    public void Configure(EntityTypeBuilder<FreelancerProfileEntity> builder)
    {
        builder.HasKey(fp => fp.Id);
        builder.HasIndex(fp => fp.UserId).IsUnique();

        builder.Property(fp => fp.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(fp => fp.Rating).HasColumnType("decimal(3,2)").HasDefaultValue(0m);
        builder.Property(fp => fp.Bio).HasMaxLength(2000);

        builder.HasMany(fp => fp.Skills)
            .WithMany(s => s.FreelancerProfiles)
            .UsingEntity(j => j.ToTable("FreelancerProfileSkills"));
    }
}
