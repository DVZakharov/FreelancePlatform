using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Proposals.RejectProposal;

public static class RejectProposalEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/proposals/{id}/reject"), Tags("Proposals")]
    [EndpointSummary("Отклонить отклик")]
    [EndpointDescription("Заказчик отклоняет отклик фрилансера. Проект остаётся открытым для других откликов.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<RejectProposalRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new RejectProposalRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<object>.BadRequest(
                ProposalTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var proposal = await db.Proposals
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id), ct);

        if (proposal is null)
            return Result<object>.NotFound(ProposalTextResponses.NotFound).ToHttpResult();

        if (proposal.Project.ClientId != currentUserId)
            return Result<object>.Forbidden(ProposalTextResponses.NotOwnerOfProject).ToHttpResult();

        if (proposal.Status != ProposalStatus.Pending)
            return Result<object>.Conflict(ProposalTextResponses.NotPending).ToHttpResult();

        proposal.Status = ProposalStatus.Rejected;

        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, ProposalTextResponses.Rejected).ToHttpResult();
    }
}