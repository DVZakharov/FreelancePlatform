using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Proposals.GetProposalsForProject;

public class GetProposalsForProjectValidator : AbstractValidator<GetProposalsForProjectRequest>
{
    public GetProposalsForProjectValidator()
    {
        RuleFor(x => x.ProposalId).MustBeValidGuid();
    }
}