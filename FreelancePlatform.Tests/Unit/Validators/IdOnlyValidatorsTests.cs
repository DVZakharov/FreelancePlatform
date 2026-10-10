using FreelancePlatform.Application.Features.Milestones.ApproveMilestone;
using FreelancePlatform.Application.Features.Milestones.FundMilestone;
using FreelancePlatform.Application.Features.Milestones.SubmitMilestone;
using FreelancePlatform.Application.Features.Proposals.AcceptProposal;
using FreelancePlatform.Application.Features.Proposals.RejectProposal;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class IdOnlyValidatorsTests
{
    public static TheoryData<string> BadIds => ["", "   ", "not-a-guid", "123"];

    [Theory, MemberData(nameof(BadIds))]
    public void Fund_RejectsInvalidId(string id) =>
        new FundMilestoneValidator().FailsOn(new FundMilestoneRequest(id), nameof(FundMilestoneRequest.MilestoneId));

    [Fact]
    public void Fund_AcceptsValidGuid() =>
        new FundMilestoneValidator().Passes(new FundMilestoneRequest(Guid.NewGuid().ToString()));

    [Theory, MemberData(nameof(BadIds))]
    public void Submit_RejectsInvalidId(string id) =>
        new SubmitMilestoneValidator().FailsOn(new SubmitMilestoneRequest(id), nameof(SubmitMilestoneRequest.MilestoneId));

    [Fact]
    public void Submit_AcceptsValidGuid() =>
        new SubmitMilestoneValidator().Passes(new SubmitMilestoneRequest(Guid.NewGuid().ToString()));
    
    [Theory, MemberData(nameof(BadIds))]
    public void Approve_RejectsInvalidId(string id) =>
        new ApproveMilestoneValidator().FailsOn(new ApproveMilestoneRequest(id), nameof(ApproveMilestoneRequest.MilestoneId));

    [Fact]
    public void Approve_AcceptsValidGuid() =>
        new ApproveMilestoneValidator().Passes(new ApproveMilestoneRequest(Guid.NewGuid().ToString()));
    
    [Theory, MemberData(nameof(BadIds))]
    public void AcceptProposal_RejectsInvalidId(string id) =>
        new AcceptProposalValidator().FailsOn(new AcceptProposalRequest(id), nameof(AcceptProposalRequest.ProposalId));

    [Fact]
    public void AcceptProposal_AcceptsValidGuid() =>
        new AcceptProposalValidator().Passes(new AcceptProposalRequest(Guid.NewGuid().ToString()));
    
    [Theory, MemberData(nameof(BadIds))]
    public void RejectProposal_RejectsInvalidId(string id) =>
        new RejectProposalValidator().FailsOn(new RejectProposalRequest(id), nameof(RejectProposalRequest.ProposalId));

    [Fact]
    public void RejectProposal_AcceptsValidGuid() =>
        new RejectProposalValidator().Passes(new RejectProposalRequest(Guid.NewGuid().ToString()));
}