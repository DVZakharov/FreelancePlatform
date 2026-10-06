using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Proposals.GetMyProposals;

public static class GetMyProposalsEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/users/me/proposals"), Tags("Proposals")]
    [EndpointSummary("Мои отклики")]
    [EndpointDescription("Возвращает все отклики текущего пользователя как фрилансера, независимо от статуса.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var proposals = await db.Proposals
            .Include(p => p.Project)
            .Include(p => p.Freelancer)
            .Where(p => p.FreelancerId == currentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var dtos = proposals.Select(ProposalDto.FromEntity).ToList();

        return Result<List<ProposalDto>>.Ok(dtos, ProposalTextResponses.GetSuccess).ToHttpResult();
    }
}