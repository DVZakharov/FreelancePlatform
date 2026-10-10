using FreelancePlatform.Application.Features.Milestones.CreateMilestone;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class CreateMilestoneValidatorTests
{
    private readonly CreateMilestoneValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_IsInvalid(string title) =>
        _validator.FailsOn(new CreateMilestoneRequest(title, 100, null), nameof(CreateMilestoneRequest.Title));

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void TitleLength_Boundary(int length, bool valid)
    {
        var request = new CreateMilestoneRequest(new string('a', length), 100, null);

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(CreateMilestoneRequest.Title));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void NonPositiveAmount_IsInvalid(double amount) =>
        _validator.FailsOn(new CreateMilestoneRequest("Этап", (decimal)amount, null), nameof(CreateMilestoneRequest.Amount));

    [Fact]
    public void SmallestPositiveAmount_IsValid() =>
        _validator.Passes(new CreateMilestoneRequest("Этап", 0.01m, null));

    [Fact]
    public void WithoutDueDate_IsValid() =>
        _validator.Passes(new CreateMilestoneRequest("Этап", 100, null));
}