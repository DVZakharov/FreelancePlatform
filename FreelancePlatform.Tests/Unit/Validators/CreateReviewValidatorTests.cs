using FreelancePlatform.Application.Features.Reviews.CreateReview;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class CreateReviewValidatorTests
{
    private readonly CreateReviewValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void RatingOutOfRange_IsInvalid(int rating) =>
        Assert.False(_validator.Validate(new CreateReviewRequest(rating, null)).IsValid);

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void CommentLength_Boundary(int length, bool valid)
    {
        var request = new CreateReviewRequest(5, new string('a', length));

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(CreateReviewRequest.Comment));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RatingInRange_IsValid(int rating) =>
        Assert.True(_validator.Validate(new CreateReviewRequest(rating, null)).IsValid);
}