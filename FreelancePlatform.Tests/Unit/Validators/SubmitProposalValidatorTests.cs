using FreelancePlatform.Application.Features.Proposals.SubmitProposal;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class SubmitProposalValidatorTests
{
    private readonly SubmitProposalValidator _validator = new();

    [Theory]
    [InlineData(0, 5, "text")]
    [InlineData(-10, 5, "text")]
    [InlineData(100, 0, "text")]
    [InlineData(100, 5, "")]
    public void InvalidInput_IsRejected(decimal price, int days, string letter) =>
        Assert.False(_validator.Validate(new SubmitProposalRequest(price, days, letter)).IsValid);

    [Theory]
    [InlineData(3000, true)]
    [InlineData(3001, false)]
    public void CoverLetterLength_Boundary(int length, bool valid)
    {
        var request = new SubmitProposalRequest(100, 5, new string('a', length));

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(SubmitProposalRequest.CoverLetter));
    }
    
    [Fact]
    public void ValidInput_IsAccepted() =>
        Assert.True(_validator.Validate(new SubmitProposalRequest(100, 5, "text")).IsValid);
}