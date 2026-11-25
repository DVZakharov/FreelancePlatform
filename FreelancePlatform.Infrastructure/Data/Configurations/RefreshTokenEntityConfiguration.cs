using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
        builder.HasIndex(r => r.Token).IsUnique();
        
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.UserId);
    }
}
