using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class ContractEntityConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AgreedPrice).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.Project)
            .WithOne(p => p.Contract)
            .HasForeignKey<ContractEntity>(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ProjectId).IsUnique();

        builder.HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Freelancer)
            .WithMany()
            .HasForeignKey(c => c.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Milestones)
            .WithOne(m => m.Contract)
            .HasForeignKey(m => m.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Disputes)
            .WithOne(d => d.Contract)
            .HasForeignKey(d => d.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Reviews)
            .WithOne(r => r.Contract)
            .HasForeignKey(r => r.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Contract)
            .HasForeignKey(m => m.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}