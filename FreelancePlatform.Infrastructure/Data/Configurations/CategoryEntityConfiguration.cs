using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class CategoryEntityConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(s => s.Name).HasColumnType("citext").IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(c => c.Projects)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}