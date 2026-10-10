using FreelancePlatform.Application.Features.Projects.CreateProject;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    private static CreateProjectRequest Valid() => new(
        Guid.NewGuid().ToString(), "Сайт на ASP.NET", "Описание", "Fixed",
        100, 500, DateTime.UtcNow.AddDays(7), [Guid.NewGuid().ToString()]);

    [Fact]
    public void Valid_Passes() => _validator.Passes(Valid());

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void BadCategoryId_Fails(string id) =>
        _validator.FailsOn(Valid() with { CategoryId = id }, nameof(CreateProjectRequest.CategoryId));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_Fails(string title) =>
        _validator.FailsOn(Valid() with { Title = title }, nameof(CreateProjectRequest.Title));

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void TitleLength_Boundary(int length, bool valid)
    {
        var request = Valid() with { Title = new string('a', length) };

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(CreateProjectRequest.Title));
    }

    [Theory]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void DescriptionLength_Boundary(int length, bool valid)
    {
        var request = Valid() with { Description = new string('a', length) };

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(CreateProjectRequest.Description));
    }

    [Theory]
    [InlineData("Weekly")]
    [InlineData("")]
    public void UnknownBudgetType_Fails(string type) =>
        _validator.FailsOn(Valid() with { BudgetType = type }, nameof(CreateProjectRequest.BudgetType));

    [Theory]
    [InlineData("Fixed")]
    [InlineData("fixed")]
    [InlineData("Hourly")]
    public void KnownBudgetType_IgnoringCase_Passes(string type) =>
        _validator.Passes(Valid() with { BudgetType = type });

    [Fact]
    public void BudgetMaxBelowMin_Fails() =>
        _validator.FailsOn(Valid() with { BudgetMin = 500, BudgetMax = 499 }, nameof(CreateProjectRequest.BudgetMax));

    [Fact]
    public void BudgetMaxEqualToMin_Passes() =>
        _validator.Passes(Valid() with { BudgetMin = 500, BudgetMax = 500 });

    [Fact]
    public void NegativeBudgetMin_Fails() =>
        _validator.FailsOn(Valid() with { BudgetMin = -1 }, nameof(CreateProjectRequest.BudgetMin));

    [Fact]
    public void MalformedSkillId_Fails()
    {
        var request = Valid() with { SkillIds = [Guid.NewGuid().ToString(), "nope"] };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith(nameof(CreateProjectRequest.SkillIds)));
    }

    // Черновик можно сохранить неполным, строгая проверка только при публикации
    [Fact]
    public void Draft_MayHaveEmptyDescription_NoSkills_AndPastDeadline() =>
        _validator.Passes(Valid() with
        {
            Description = "",
            SkillIds = [],
            Deadline = DateTime.UtcNow.AddDays(-1),
            BudgetMin = 0,
            BudgetMax = 0
        });
}