using FreelancePlatform.Application.Features.Payments.TopUpBalance;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class TopUpBalanceValidatorTests
{
    private readonly TopUpBalanceValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void OutOfRange_Fails(int amount) =>
        _validator.FailsOn(new TopUpBalanceRequest(amount), nameof(TopUpBalanceRequest.Amount));

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void InRange_Passes(double amount) =>
        _validator.Passes(new TopUpBalanceRequest((decimal)amount));
}