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

namespace FreelancePlatform.Application.Features.Projects.CancelProject;

public static class CancelProjectEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/projects/{id}/cancel"), Tags("Projects")]
    [EndpointSummary("Отмена проекта")]
    [EndpointDescription("Отменяет проект. Доступно только владельцу, только пока проект в статусе " +
        "Draft или Open. Если у проекта уже есть отклики, все они автоматически переводятся в Rejected. " +
        "Проекты в статусе InProgress отменить нельзя — только через разрешение спора по контракту.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<CancelProjectRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new CancelProjectRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<object>.BadRequest(ProjectTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var project = await db.Projects
            .Include(p => p.Proposals)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id), ct);

        if (project is null)
            return Result<object>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();

        if (project.ClientId != currentUserId)
            return Result<object>.Forbidden(ProjectTextResponses.NotOwner).ToHttpResult();

        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.Open)
            return Result<object>.Conflict(ProjectTextResponses.CannotCancel).ToHttpResult();

        foreach (var proposal in project.Proposals.Where(p => p.Status == ProposalStatus.Pending))
            proposal.Status = ProposalStatus.Rejected;

        project.Status = ProjectStatus.Cancelled;

        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, ProjectTextResponses.Cancelled).ToHttpResult();
    }
}