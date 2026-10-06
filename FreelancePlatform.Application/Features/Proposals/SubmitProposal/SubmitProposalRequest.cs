namespace FreelancePlatform.Application.Features.Proposals.SubmitProposal;

public record SubmitProposalRequest(decimal Price, int EstimatedDays, string CoverLetter);