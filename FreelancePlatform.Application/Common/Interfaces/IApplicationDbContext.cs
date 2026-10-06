using FreelancePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserEntity> Users { get; }
    DbSet<FreelancerProfileEntity> FreelancerProfiles { get; }
    DbSet<CategoryEntity> Categories { get; }
    DbSet<SkillEntity> Skills { get; }
    DbSet<ProjectEntity> Projects { get; }
    DbSet<ProposalEntity> Proposals { get; }
    DbSet<ContractEntity> Contracts { get; }
    DbSet<MilestoneEntity> Milestones { get; }
    DbSet<EscrowHoldEntity> EscrowHolds { get; }
    DbSet<TransactionEntity> Transactions { get; }
    DbSet<DisputeEntity> Disputes { get; }
    DbSet<ReviewEntity> Reviews { get; }
    DbSet<MessageEntity> Messages { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}