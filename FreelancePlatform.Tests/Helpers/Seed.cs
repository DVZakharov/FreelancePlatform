using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Tests.Helpers;

public sealed record MilestoneScenario(Guid ClientId, Guid FreelancerId, Guid ContractId, Guid MilestoneId);
public sealed record DisputeScenario(Guid ClientId, Guid FreelancerId, Guid AdminId, Guid ContractId, Guid DisputeId);
public sealed record ProjectScenario(Guid ClientId, Guid ProjectId);

public static class Seed
{
    public static UserEntity NewUser(decimal balance = 0, UserRole role = UserRole.Client) => new()
    {
        Email = $"{Guid.NewGuid()}@test.local",
        PasswordHash = "x",
        FullName = "Test User",
        Role = role,
        Balance = balance
    };

    public static async Task<ProjectScenario> DraftProjectAsync(
        TestDb testDb,
        string title = "Сайт на ASP.NET",
        int descriptionLength = 100,
        decimal budgetMin = 100,
        decimal budgetMax = 500,
        int deadlineInDays = 7,
        int skillsCount = 1,
        ProjectStatus status = ProjectStatus.Draft)
    {
        await using var db = testDb.NewContext();

        var client = NewUser();
        var category = new CategoryEntity { Name = $"Category {Guid.NewGuid()}" };
        var project = new ProjectEntity
        {
            ClientId = client.Id,
            CategoryId = category.Id,
            Title = title,
            Description = new string('a', descriptionLength),
            BudgetType = BudgetType.Fixed,
            BudgetMin = budgetMin,
            BudgetMax = budgetMax,
            Deadline = DateTime.UtcNow.AddDays(deadlineInDays),
            Status = status
        };

        for (var i = 0; i < skillsCount; i++)
            project.RequiredSkills.Add(new SkillEntity { Name = $"Skill {Guid.NewGuid()}" });

        db.AddRange(client, category, project);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new ProjectScenario(client.Id, project.Id);
    }
    
    public static async Task<MilestoneScenario> MilestoneAsync(
        TestDb testDb,
        decimal clientBalance,
        decimal amount,
        MilestoneStatus milestoneStatus = MilestoneStatus.Pending,
        ContractStatus contractStatus = ContractStatus.Active,
        bool withEscrow = false)
    {
        await using var db = testDb.NewContext();

        var client = NewUser(clientBalance);
        var freelancer = NewUser();
        var project = new ProjectEntity
        {
            ClientId = client.Id, Title = "Project", Description = "Description",
            Status = ProjectStatus.InProgress
        };
        var contract = new ContractEntity
        {
            ProjectId = project.Id, ClientId = client.Id, FreelancerId = freelancer.Id,
            AgreedPrice = amount, AgreedDeadline = DateTime.UtcNow.AddDays(7), Status = contractStatus
        };
        var milestone = new MilestoneEntity
        {
            ContractId = contract.Id, Title = "Milestone 1", Amount = amount, Status = milestoneStatus
        };

        db.AddRange(client, freelancer, project, contract, milestone);

        if (withEscrow)
            db.EscrowHolds.Add(new EscrowHoldEntity
            {
                MilestoneId = milestone.Id, ContractId = contract.Id,
                Amount = amount, Status = HoldStatus.Held
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new MilestoneScenario(client.Id, freelancer.Id, contract.Id, milestone.Id);
    }

    public static async Task<DisputeScenario> DisputeAsync(
        TestDb testDb, decimal clientBalance, params decimal[] heldAmounts)
    {
        await using var db = testDb.NewContext();

        var client = NewUser(clientBalance);
        var freelancer = NewUser();
        var admin = NewUser(role: UserRole.Admin);
        var project = new ProjectEntity
        {
            ClientId = client.Id, Title = "Project", Description = "Description", Status = ProjectStatus.InProgress
        };
        var contract = new ContractEntity
        {
            ProjectId = project.Id, ClientId = client.Id, FreelancerId = freelancer.Id,
            AgreedPrice = heldAmounts.Sum(), AgreedDeadline = DateTime.UtcNow.AddDays(7),
            Status = ContractStatus.InDispute
        };
        var dispute = new DisputeEntity
        {
            ContractId = contract.Id, OpenedById = client.Id,
            Reason = "Работа не соответствует ТЗ", Status = DisputeStatus.Open
        };

        db.AddRange(client, freelancer, admin, project, contract, dispute);

        foreach (var amount in heldAmounts)
        {
            var milestone = new MilestoneEntity
            {
                ContractId = contract.Id, Title = $"Milestone {amount}", Amount = amount,
                Status = MilestoneStatus.Submitted
            };
            db.Add(milestone);
            db.EscrowHolds.Add(new EscrowHoldEntity
            {
                MilestoneId = milestone.Id, ContractId = contract.Id, Amount = amount, Status = HoldStatus.Held
            });
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new DisputeScenario(client.Id, freelancer.Id, admin.Id, contract.Id, dispute.Id);
    }

    public sealed record OpenProjectScenario(Guid ClientId, Guid FreelancerId, Guid ProjectId);

    public static async Task<OpenProjectScenario> OpenProjectAsync(
        TestDb testDb, bool freelancerHasProfile = true, ProjectStatus status = ProjectStatus.Open)
    {
        await using var db = testDb.NewContext();

        var client = NewUser();
        var freelancer = NewUser(role: UserRole.Client | UserRole.Freelancer);
        var project = new ProjectEntity
        {
            ClientId = client.Id, Title = "Project", Description = "Description", Status = status
        };

        db.AddRange(client, freelancer, project);
        if (freelancerHasProfile)
            db.FreelancerProfiles.Add(new FreelancerProfileEntity
            {
                UserId = freelancer.Id, Bio = "bio", HourlyRate = 10
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new OpenProjectScenario(client.Id, freelancer.Id, project.Id);
    }
}