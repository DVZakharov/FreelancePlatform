using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Role).HasConversion<int>();
        
        builder.Property(e => e.Version).IsRowVersion();

        builder.Property(u => u.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.HasOne(u => u.FreelancerProfile)
            .WithOne(fp => fp.User)
            .HasForeignKey<FreelancerProfileEntity>(fp => fp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ProjectsAsClient)
            .WithOne(p => p.Client)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Proposals)
            .WithOne(p => p.Freelancer)
            .HasForeignKey(p => p.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Transactions)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Messages)
            .WithOne(m => m.Sender)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}