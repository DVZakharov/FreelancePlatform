using FluentValidation;

namespace FreelancePlatform.Application.Features.Reviews.CreateReview;

public class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}