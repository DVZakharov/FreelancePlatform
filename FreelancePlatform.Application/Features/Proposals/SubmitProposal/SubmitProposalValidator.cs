using FluentValidation;

namespace FreelancePlatform.Application.Features.Proposals.SubmitProposal;

public class SubmitProposalValidator : AbstractValidator<SubmitProposalRequest>
{
    public SubmitProposalValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(ProposalTextResponses.PriceGreaterZero);
        
        RuleFor(x => x.EstimatedDays)
            .GreaterThan(0).WithMessage(ProposalTextResponses.EstimatedDaysGreaterZero);
        
        RuleFor(x => x.CoverLetter)
            .NotEmpty().WithMessage(ProposalTextResponses.CoverLetterNotEmpty)
            .MaximumLength(3000).WithMessage(ProposalTextResponses.CoverLetterMaxLenght);
    }
}