using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class EscrowHoldEntityConfiguration : IEntityTypeConfiguration<EscrowHoldEntity>
{
    public void Configure(EntityTypeBuilder<EscrowHoldEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Milestone)
            .WithOne(m => m.EscrowHold)
            .HasForeignKey<EscrowHoldEntity>(e => e.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.MilestoneId).IsUnique();

        builder.HasOne(e => e.Contract)
            .WithMany()
            .HasForeignKey(e => e.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}