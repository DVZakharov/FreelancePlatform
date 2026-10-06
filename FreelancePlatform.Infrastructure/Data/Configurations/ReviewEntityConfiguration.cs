using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class ReviewEntityConfiguration : IEntityTypeConfiguration<ReviewEntity>
{
    public void Configure(EntityTypeBuilder<ReviewEntity> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment).HasMaxLength(2000);

        builder.HasOne(r => r.Contract)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ContractId, r.AuthorId }).IsUnique();
    }
}