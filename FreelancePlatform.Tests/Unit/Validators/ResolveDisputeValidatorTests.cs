using FreelancePlatform.Application.Features.Disputes.ResolveDispute;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class ResolveDisputeValidatorTests
{
    private readonly ResolveDisputeValidator _validator = new();

    [Theory]
    [InlineData("Client")]
    [InlineData("Freelancer")]
    public void KnownDecision_IsValid(string decision) =>
        _validator.Passes(new ResolveDisputeRequest(decision, "Решение"));

    [Theory]
    [InlineData("")]
    [InlineData("Nobody")]
    [InlineData("Admin")]
    [InlineData("client")]   // регистр важен, см. примечание ниже
    public void UnknownDecision_IsInvalid(string decision) =>
        _validator.FailsOn(new ResolveDisputeRequest(decision, "Решение"), nameof(ResolveDisputeRequest.Decision));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyResolution_IsInvalid(string resolution) =>
        _validator.FailsOn(new ResolveDisputeRequest("Client", resolution), nameof(ResolveDisputeRequest.Resolution));

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void ResolutionLength_Boundary(int length, bool valid)
    {
        var request = new ResolveDisputeRequest("Client", new string('a', length));

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(ResolveDisputeRequest.Resolution));
    }
}