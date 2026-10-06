using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Reviews.GetFreelancerReviews;

public class GetFreelancerReviewsValidator : AbstractValidator<GetFreelancerReviewsRequest>
{
    public GetFreelancerReviewsValidator()
    {
        RuleFor(x => x.FreelancerId).MustBeValidGuid();
    }
}