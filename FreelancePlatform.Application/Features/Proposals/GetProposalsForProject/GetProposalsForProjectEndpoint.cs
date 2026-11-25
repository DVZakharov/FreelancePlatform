using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Proposals.GetProposalsForProject;

public static class GetProposalsForProjectEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/projects/{projectId}/proposals"), Tags("Proposals")]
    [EndpointSummary("Отклики на проект")]
    [EndpointDescription("Возвращает все отклики на указанный проект. Доступно только владельцу проекта.")]
    public static async Task<IResult> Handle(
        [FromRoute] string projectId,
        IValidator<GetProposalsForProjectRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetProposalsForProjectRequest(projectId);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<List<ProposalDto>>.BadRequest(
                ProposalTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == Guid.Parse(projectId), ct);
        if (project is null)
            return Result<List<ProposalDto>>.NotFound(ProposalTextResponses.ProjectNotFound).ToHttpResult();

        if (project.ClientId != currentUserId)
            return Result<List<ProposalDto>>.Forbidden(ProposalTextResponses.NotOwnerOfProject).ToHttpResult();

        var proposals = await db.Proposals
            .Include(p => p.Project)
            .Include(p => p.Freelancer)
            .Where(p => p.ProjectId == Guid.Parse(projectId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var dtos = proposals.Select(ProposalDto.FromEntity).ToList();

        return Result<List<ProposalDto>>.Ok(dtos, ProposalTextResponses.GetSuccess).ToHttpResult();
    }
}