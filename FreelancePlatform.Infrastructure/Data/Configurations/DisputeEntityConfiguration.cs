using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class DisputeEntityConfiguration : IEntityTypeConfiguration<DisputeEntity>
{
    public void Configure(EntityTypeBuilder<DisputeEntity> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.Resolution).HasMaxLength(2000);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(d => d.Contract)
            .WithMany(c => c.Disputes)
            .HasForeignKey(d => d.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}