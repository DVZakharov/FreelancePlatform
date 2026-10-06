using FluentValidation;
using FreelancePlatform.Application.Common.Validators;

namespace FreelancePlatform.Application.Features.Proposals.RejectProposal;

public class RejectProposalValidator : AbstractValidator<RejectProposalRequest>
{
    public RejectProposalValidator()
    {
        RuleFor(x => x.ProposalId).MustBeValidGuid();
    }
}