using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreelancePlatform.Infrastructure.Data.Configurations;

public class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(m => m.Contract)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ContractId);
    }
}