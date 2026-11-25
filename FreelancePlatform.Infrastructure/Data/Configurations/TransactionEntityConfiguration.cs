using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class TransactionEntityConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Milestone)
            .WithMany(m => m.Transactions)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(t => t.UserId);
    }
}