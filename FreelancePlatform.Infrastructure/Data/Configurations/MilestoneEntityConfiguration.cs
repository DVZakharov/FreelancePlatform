using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class MilestoneEntityConfiguration : IEntityTypeConfiguration<MilestoneEntity>
{
    public void Configure(EntityTypeBuilder<MilestoneEntity> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Amount).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Version).IsRowVersion();
        
        builder.HasOne(m => m.Contract)
            .WithMany(c => c.Milestones)
            .HasForeignKey(m => m.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.EscrowHold)
            .WithOne(e => e.Milestone)
            .HasForeignKey<EscrowHoldEntity>(e => e.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Transactions)
            .WithOne(t => t.Milestone)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}