using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.FreelancerProfiles.SearchProfiles;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.FreelancerProfiles;

public class SearchProfilesTests
{
    private readonly TestDb _testDb = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SkillEntity _csharp = null!, _docker = null!, _sql = null!;

    // alice: C# + Docker, 4.9 | bob: C#, 3.0 | carol: C# + Docker + SQL, 4.0
    private async Task SeedAsync()
    {
        await using var db = _testDb.NewContext();
        _csharp = new SkillEntity { Name = "C#" };
        _docker = new SkillEntity { Name = "Docker" };
        _sql = new SkillEntity { Name = "SQL" };
        db.Skills.AddRange(_csharp, _docker, _sql);

        void Add(string name, decimal rating, params SkillEntity[] skills)
        {
            var user = Seed.NewUser();
            user.FullName = name;
            db.Users.Add(user);
            db.FreelancerProfiles.Add(new FreelancerProfileEntity
            {
                UserId = user.Id, Bio = "bio", HourlyRate = 10, Rating = rating, Skills = [.. skills]
            });
        }

        Add("alice", 4.9m, _csharp, _docker);
        Add("bob", 3.0m, _csharp);
        Add("carol", 4.0m, _csharp, _docker, _sql);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<ListFreelancerProfilesDto> SearchAsync(SearchProfilesRequest request)
    {
        var response = await _testDb.RunAsync(db => SearchProfilesEndpoint.Handle(
            request, new SearchProfilesValidator(), db, Ct));
        var result = ResultAssert.Unwrap<ListFreelancerProfilesDto>(response);
        Assert.Equal(200, result.Code);
        return result.Value!;
    }

    private static List<string> Names(ListFreelancerProfilesDto r) => [.. r.Items.Select(p => p.FullName)];

    [Fact]
    public async Task Search_WithoutFilters_ReturnsAll_SortedByRatingDescending()
    {
        await SeedAsync();

        var result = await SearchAsync(new SearchProfilesRequest { PageNumber = 1, PageSize = 20 });

        Assert.Equal(["alice", "carol", "bob"], Names(result));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Search_ByTwoSkills_RequiresAll_NotAny()
    {
        await SeedAsync();

        var result = await SearchAsync(new SearchProfilesRequest
        {
            SkillIds = [_csharp.Id.ToString(), _docker.Id.ToString()], PageNumber = 1, PageSize = 20
        });

        // bob знает только C#, поэтому в выдачу не попадает
        Assert.Equal(["alice", "carol"], Names(result));
    }

    [Fact]
    public async Task Search_ByThreeSkills_ReturnsOnlyTheOneWhoKnowsAll()
    {
        await SeedAsync();

        var result = await SearchAsync(new SearchProfilesRequest
        {
            SkillIds = [_csharp.Id.ToString(), _docker.Id.ToString(), _sql.Id.ToString()],
            PageNumber = 1, PageSize = 20
        });

        Assert.Equal(["carol"], Names(result));
    }

    [Fact]
    public async Task Search_ByMinRating_IsInclusive()
    {
        await SeedAsync();

        var result = await SearchAsync(new SearchProfilesRequest { MinRating = 4.0m, PageNumber = 1, PageSize = 20 });

        Assert.Equal(["alice", "carol"], Names(result));   // 4.0 входит, 3.0 нет
    }

    [Fact]
    public async Task Search_Pagination_SplitsResults_AndKeepsTotalCount()
    {
        await SeedAsync();

        var page2 = await SearchAsync(new SearchProfilesRequest { PageNumber = 2, PageSize = 2 });

        Assert.Equal(["bob"], Names(page2));
        Assert.Equal(3, page2.TotalCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5.01)]
    public async Task Search_WithRatingOutOfRange_Returns400(double rating)
    {
        var response = await _testDb.RunAsync(db => SearchProfilesEndpoint.Handle(
            new SearchProfilesRequest { MinRating = (decimal)rating, PageNumber = 1, PageSize = 20 },
            new SearchProfilesValidator(), db, Ct));

        Assert.Equal(400, ResultAssert.Unwrap<ListFreelancerProfilesDto>(response).Code);
    }

    [Fact]
    public async Task Search_WithMalformedSkillId_Returns400()
    {
        var response = await _testDb.RunAsync(db => SearchProfilesEndpoint.Handle(
            new SearchProfilesRequest { SkillIds = ["nope"], PageNumber = 1, PageSize = 20 },
            new SearchProfilesValidator(), db, Ct));

        Assert.Equal(400, ResultAssert.Unwrap<ListFreelancerProfilesDto>(response).Code);
    }
}