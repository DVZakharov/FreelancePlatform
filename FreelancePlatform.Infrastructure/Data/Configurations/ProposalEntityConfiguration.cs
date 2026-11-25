using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class ProposalEntityConfiguration : IEntityTypeConfiguration<ProposalEntity>
{
    public void Configure(EntityTypeBuilder<ProposalEntity> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CoverLetter).HasMaxLength(3000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Project)
            .WithMany(pr => pr.Proposals)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Freelancer)
            .WithMany(u => u.Proposals)
            .HasForeignKey(p => p.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ProjectId, p.FreelancerId }).IsUnique();
    }
}