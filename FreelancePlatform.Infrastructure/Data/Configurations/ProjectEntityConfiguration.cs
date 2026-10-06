using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class ProjectEntityConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(5000);

        builder.Property(p => p.BudgetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.BudgetMin).HasColumnType("decimal(18,2)");
        builder.Property(p => p.BudgetMax).HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Client)
            .WithMany(u => u.ProjectsAsClient)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.RequiredSkills)
            .WithMany(s => s.Projects)
            .UsingEntity(j => j.ToTable("ProjectSkills"));

        builder.Property(p => p.SelectedProposalId);

        builder.HasIndex(p => p.Status);
    }
}