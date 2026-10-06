using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<FreelancerProfileEntity> FreelancerProfiles => Set<FreelancerProfileEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<ProposalEntity> Proposals => Set<ProposalEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<MilestoneEntity> Milestones => Set<MilestoneEntity>();
    public DbSet<EscrowHoldEntity> EscrowHolds => Set<EscrowHoldEntity>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<DisputeEntity> Disputes => Set<DisputeEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamptz");
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}