using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Proposals.AcceptProposal;

public class AcceptProposalValidator : AbstractValidator<AcceptProposalRequest>
{
    public AcceptProposalValidator()
    {
        RuleFor(x => x.ProposalId).MustBeValidGuid();
    }
}