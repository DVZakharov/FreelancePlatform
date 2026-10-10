using FreelancePlatform.Application.Features.Disputes.OpenDispute;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class OpenDisputeValidatorTests
{
    private readonly OpenDisputeValidator _validator = new();

    [Theory]
    [InlineData(0, false)]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void ReasonLength_Boundaries(int length, bool valid)
    {
        var request = new OpenDisputeRequest(new string('a', length));

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(OpenDisputeRequest.Reason));
    }
}