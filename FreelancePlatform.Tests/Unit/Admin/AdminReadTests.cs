using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Admin.GetPlatformStats;
using FreelancePlatform.Application.Features.Admin.GetUserById;
using FreelancePlatform.Application.Features.Admin.SearchDisputes;
using FreelancePlatform.Application.Features.Admin.SearchUsers;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Admin;

public class AdminReadTests
{
    private readonly TestDb _testDb = new();

    // ================= GetPlatformStats =================

    [Fact(Timeout = 10_000)]
    public async Task Stats_OnEmptyPlatform_ReturnsZeros_NotNulls()
    {
        var response = await _testDb.RunAsync(db => GetPlatformStatsEndpoint.Handle(db, TestContext.Current.CancellationToken));

        var stats = ResultAssert.Unwrap<PlatformStatsDto>(response).Value!;
        Assert.Equal(0, stats.TotalUsers);
        Assert.Equal(0m, stats.TotalEscrowHeld);
        Assert.Equal(0m, stats.TotalUsersBalance);
        Assert.Equal(0m, stats.TotalToppedUp);
        Assert.Empty(stats.ProjectsByStatus);
    }

    [Fact(Timeout = 10_000)]
    public async Task Stats_AggregatesUsersProjectsContractsDisputesAndMoney()
    {
        // клиент (баланс 500), фрилансер, админ; контракт InDispute; открытый спор; два холда 100 и 50
        var s = await Seed.DisputeAsync(_testDb, 500, 100, 50);
        await using (var db = _testDb.NewContext())
        {
            db.FreelancerProfiles.Add(new FreelancerProfileEntity { UserId = s.FreelancerId, Bio = "bio", HourlyRate = 10 });

            foreach (var status in new[] { ProjectStatus.Open, ProjectStatus.Open, ProjectStatus.Draft })
                db.Projects.Add(new ProjectEntity { ClientId = s.ClientId, Title = "p", Description = "d", Status = status });

            foreach (var status in new[] { ContractStatus.Active, ContractStatus.Active, ContractStatus.Completed })
                db.Contracts.Add(new ContractEntity
                {
                    ProjectId = Guid.NewGuid(), ClientId = s.ClientId, FreelancerId = s.FreelancerId,
                    AgreedPrice = 10, AgreedDeadline = DateTime.UtcNow.AddDays(1), Status = status
                });

            // разрешённый спор и закрытый холд в статистику «открыто/заморожено» не попадают
            db.Disputes.Add(new DisputeEntity
            {
                ContractId = s.ContractId, OpenedById = s.ClientId, Reason = "старый",
                Status = DisputeStatus.ResolvedForClient
            });
            db.EscrowHolds.Add(new EscrowHoldEntity
            {
                MilestoneId = Guid.NewGuid(), ContractId = s.ContractId, Amount = 70, Status = HoldStatus.Refunded
            });

            // пополнения считаются, остальные типы нет
            db.Transactions.AddRange(
                new TransactionEntity { UserId = s.ClientId, Type = TransactionType.TopUp, Amount = 200 },
                new TransactionEntity { UserId = s.ClientId, Type = TransactionType.TopUp, Amount = 300 },
                new TransactionEntity { UserId = s.ClientId, Type = TransactionType.Freeze, Amount = 150 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db => GetPlatformStatsEndpoint.Handle(db, TestContext.Current.CancellationToken));

        var stats = ResultAssert.Unwrap<PlatformStatsDto>(response).Value!;
        Assert.Equal(3, stats.TotalUsers);
        Assert.Equal(1, stats.TotalAdmins);
        Assert.Equal(1, stats.TotalFreelancers);
        Assert.Equal(2, stats.ProjectsByStatus["Open"]);
        Assert.Equal(1, stats.ProjectsByStatus["Draft"]);
        Assert.Equal(1, stats.ProjectsByStatus["InProgress"]);
        Assert.Equal(2, stats.ContractsByStatus["Active"]);
        Assert.Equal(1, stats.ContractsByStatus["Completed"]);
        Assert.Equal(1, stats.ContractsByStatus["InDispute"]);
        Assert.Equal(1, stats.OpenDisputes);
        Assert.Equal(150m, stats.TotalEscrowHeld);
        Assert.Equal(500m, stats.TotalUsersBalance);
        Assert.Equal(500m, stats.TotalToppedUp);
    }

    // ================= GetUserById =================

    [Fact(Timeout = 10_000)]
    public async Task UserDetails_CountsOnlyThisUsersActivity_AndOnlyActiveSessions()
    {
        UserEntity user;
        await using (var db = _testDb.NewContext())
        {
            user = Seed.NewUser(balance: 40, role: UserRole.Client | UserRole.Freelancer);
            var other = Seed.NewUser();
            db.AddRange(user, other);
            db.FreelancerProfiles.Add(new FreelancerProfileEntity { UserId = user.Id, Bio = "bio", HourlyRate = 10 });

            var projectA = new ProjectEntity { ClientId = user.Id, Title = "a", Description = "d" };
            var projectB = new ProjectEntity { ClientId = user.Id, Title = "b", Description = "d" };
            var foreign = new ProjectEntity { ClientId = other.Id, Title = "c", Description = "d" };
            var foreignToo = new ProjectEntity { ClientId = other.Id, Title = "d", Description = "d" };
            db.AddRange(projectA, projectB, foreign, foreignToo);

            db.Proposals.Add(new ProposalEntity { ProjectId = foreign.Id, FreelancerId = user.Id, Price = 1, EstimatedDays = 1 });
            db.Proposals.Add(new ProposalEntity { ProjectId = projectA.Id, FreelancerId = other.Id, Price = 1, EstimatedDays = 1 });

            // у проекта не может быть больше одного контракта (1:1, уникальный индекс), поэтому проекты разные
            db.Contracts.AddRange(
                new ContractEntity { ProjectId = projectA.Id, ClientId = user.Id, FreelancerId = other.Id, AgreedDeadline = DateTime.UtcNow },
                new ContractEntity { ProjectId = foreign.Id, ClientId = other.Id, FreelancerId = user.Id, AgreedDeadline = DateTime.UtcNow },
                new ContractEntity { ProjectId = foreignToo.Id, ClientId = other.Id, FreelancerId = other.Id, AgreedDeadline = DateTime.UtcNow });

            db.RefreshTokens.AddRange(
                new RefreshTokenEntity { UserId = user.Id, Token = "active", ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshTokenEntity { UserId = user.Id, Token = "revoked", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow },
                new RefreshTokenEntity { UserId = user.Id, Token = "expired", ExpiresAt = DateTime.UtcNow.AddMinutes(-1) },
                new RefreshTokenEntity { UserId = other.Id, Token = "foreign", ExpiresAt = DateTime.UtcNow.AddDays(1) });

            db.Transactions.AddRange(
                new TransactionEntity { UserId = user.Id, Type = TransactionType.TopUp, Amount = 1 },
                new TransactionEntity { UserId = user.Id, Type = TransactionType.Freeze, Amount = 1 },
                new TransactionEntity { UserId = other.Id, Type = TransactionType.TopUp, Amount = 1 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db => GetUserByIdEndpoint.Handle(user.Id.ToString(), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<AdminUserDetailsDto>(response);
        Assert.Equal(200, result.Code);
        var details = result.Value!;
        Assert.Equal(user.Email, details.User.Email);
        Assert.Equal(40m, details.User.Balance);
        Assert.True(details.User.IsFreelancer);
        Assert.Equal(2, details.ProjectsAsClientCount);
        Assert.Equal(1, details.ProposalsCount);
        Assert.Equal(2, details.ContractsCount);
        Assert.Equal(1, details.ActiveSessionsCount);
        Assert.Equal(2, details.TransactionsCount);
    }

    [Fact(Timeout = 10_000)]
    public async Task UserDetails_Unknown_Returns404()
    {
        var response = await _testDb.RunAsync(db => GetUserByIdEndpoint.Handle(Guid.NewGuid().ToString(), db, TestContext.Current.CancellationToken));

        Assert.Equal(404, ResultAssert.Unwrap<AdminUserDetailsDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task UserDetails_InvalidId_Returns400()
    {
        var response = await _testDb.RunAsync(db => GetUserByIdEndpoint.Handle("not-a-guid", db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<AdminUserDetailsDto>(response).Code);
    }

    // ================= SearchUsers =================

    private async Task SeedUsersAsync()
    {
        await using var db = _testDb.NewContext();
        UserEntity User(string email, string name, UserRole role, int minutesAgo)
        {
            var u = Seed.NewUser(role: role);
            u.Email = email; u.FullName = name; u.CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo);
            return u;
        }

        var anna = User("anna@shop.test", "Анна Петрова", UserRole.Client, 1);
        var boris = User("boris@work.test", "Борис Иванов", UserRole.Client | UserRole.Freelancer, 2);
        var admin = User("root@platform.test", "Администратор", UserRole.Admin, 3);
        db.AddRange(anna, boris, admin);
        db.FreelancerProfiles.Add(new FreelancerProfileEntity { UserId = boris.Id, Bio = "bio", HourlyRate = 10 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<SearchUsersResponse> SearchUsersAsync(SearchUsersRequest request, CancellationToken ct)
    {
        var response = await _testDb.RunAsync(db => SearchUsersEndpoint.Handle(request, new SearchUsersValidator(), db, ct));
        var result = ResultAssert.Unwrap<SearchUsersResponse>(response);
        Assert.Equal(200, result.Code);
        return result.Value!;
    }

    [Theory(Timeout = 10_000)]
    [InlineData("SHOP.test", "anna@shop.test")]          // по email, без учёта регистра
    [InlineData("иванов", "boris@work.test")]            // по имени, без учёта регистра
    [InlineData("  anna  ", "anna@shop.test")]           // пробелы по краям обрезаются
    public async Task SearchUsers_BySubstringOfEmailOrName(string search, string expectedEmail)
    {
        await SeedUsersAsync();

        var result = await SearchUsersAsync(new SearchUsersRequest { Search = search }, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { expectedEmail }, result.Items.Select(u => u.Email).ToArray());
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Admin", new[] { "root@platform.test" })]
    [InlineData("freelancer", new[] { "boris@work.test" })]
    [InlineData("Client", new[] { "anna@shop.test", "boris@work.test" })]
    public async Task SearchUsers_ByRole_MatchesFlags(string role, string[] expected)
    {
        await SeedUsersAsync();

        var result = await SearchUsersAsync(new SearchUsersRequest { Role = role }, TestContext.Current.CancellationToken);

        Assert.Equal(expected.Order(StringComparer.Ordinal).ToArray(),
            result.Items.Select(u => u.Email).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task SearchUsers_ReportsFreelancerFlag_AndPaginatesNewestFirst()
    {
        await SeedUsersAsync();

        var result = await SearchUsersAsync(new SearchUsersRequest { PageNumber = 2, PageSize = 2 }, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.ReturnedCount);
        Assert.Equal(new[] { "root@platform.test" }, result.Items.Select(u => u.Email).ToArray());

        var all = await SearchUsersAsync(new SearchUsersRequest(), TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "anna@shop.test", "boris@work.test", "root@platform.test" },
            all.Items.Select(u => u.Email).ToArray());   // от новых к старым
        Assert.True(all.Items.Single(u => u.Email == "boris@work.test").IsFreelancer);
        Assert.False(all.Items.Single(u => u.Email == "anna@shop.test").IsFreelancer);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("None", 1, 10, null)]
    [InlineData("Boss", 1, 10, null)]
    [InlineData(null, 0, 10, null)]
    [InlineData(null, 1, 0, null)]
    [InlineData(null, 1, 10, "TOO_LONG")]
    public async Task SearchUsers_InvalidRequest_Returns400(string? role, int page, int size, string? search)
    {
        var request = new SearchUsersRequest
        {
            Role = role, PageNumber = page, PageSize = size,
            Search = search == "TOO_LONG" ? new string('a', 101) : search
        };

        var response = await _testDb.RunAsync(db => SearchUsersEndpoint.Handle(request, new SearchUsersValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<SearchUsersResponse>(response).Code);
    }

    // ================= SearchDisputes =================

    private async Task SeedDisputesAsync()
    {
        await using var db = _testDb.NewContext();
        var statuses = new[]
        {
            DisputeStatus.Open, DisputeStatus.Open, DisputeStatus.ResolvedForClient,
            DisputeStatus.ResolvedForFreelancer, DisputeStatus.Closed
        };
        for (var i = 0; i < statuses.Length; i++)
            db.Disputes.Add(new DisputeEntity
            {
                ContractId = Guid.NewGuid(), OpenedById = Guid.NewGuid(), Reason = $"Причина {i}",
                Status = statuses[i], CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<SearchDisputesResponse> SearchDisputesAsync(SearchDisputesRequest request, CancellationToken ct)
    {
        var response = await _testDb.RunAsync(db => SearchDisputesEndpoint.Handle(request, new SearchDisputesValidator(), db, ct));
        var result = ResultAssert.Unwrap<SearchDisputesResponse>(response);
        Assert.Equal(200, result.Code);
        return result.Value!;
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Open", 2)]
    [InlineData("open", 2)]
    [InlineData("ResolvedForClient", 1)]
    [InlineData("Closed", 1)]
    public async Task SearchDisputes_FiltersByStatus_IgnoringCase(string status, int expected)
    {
        await SeedDisputesAsync();

        var result = await SearchDisputesAsync(new SearchDisputesRequest { Status = status }, TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.TotalCount);
        Assert.Equal(expected, result.Items.Count);
    }

    [Fact(Timeout = 10_000)]
    public async Task SearchDisputes_WithoutFilter_ReturnsAll_PaginatedNewestFirst()
    {
        await SeedDisputesAsync();

        var page = await SearchDisputesAsync(new SearchDisputesRequest { PageNumber = 2, PageSize = 2 }, TestContext.Current.CancellationToken);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.ReturnedCount);
        Assert.Equal(new[] { "Причина 2", "Причина 3" }, page.Items.Select(d => d.Reason).ToArray());
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Pending", 1, 10)]
    [InlineData(null, 0, 10)]
    [InlineData(null, 1, 0)]
    public async Task SearchDisputes_InvalidRequest_Returns400(string? status, int page, int size)
    {
        var request = new SearchDisputesRequest { Status = status, PageNumber = page, PageSize = size };

        var response = await _testDb.RunAsync(db => SearchDisputesEndpoint.Handle(request, new SearchDisputesValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<SearchDisputesResponse>(response).Code);
    }
}