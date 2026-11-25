using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Proposals.SubmitProposal;

public static class SubmitProposalEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/projects/{projectId}/proposals"), Tags("Proposals")]
    [EndpointSummary("Отклик на проект")]
    [EndpointDescription("Фрилансер откликается на открытый проект, предлагая свою цену и срок. " +
        "Требует наличия профиля фрилансера. Один фрилансер может оставить только один отклик на проект.")]
    public static async Task<IResult> Handle(
        [FromRoute] string projectId,
        [FromBody] SubmitProposalRequest request,
        HttpContext ctx,
        IValidator<SubmitProposalRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProposalDto>.BadRequest(
                ProposalTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(projectId, out var projectGuid))
            return Result<ProposalDto>.BadRequest(ProposalTextResponses.ValidationError, ["Invalid project id"]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectGuid, ct);
        if (project is null)
            return Result<ProposalDto>.NotFound(ProposalTextResponses.ProjectNotFound).ToHttpResult();

        if (project.Status != ProjectStatus.Open)
            return Result<ProposalDto>.Conflict(ProposalTextResponses.ProjectNotOpen).ToHttpResult();

        if (project.ClientId == currentUserId)
            return Result<ProposalDto>.BadRequest(ProposalTextResponses.CannotApplyToOwnProject).ToHttpResult();

        var hasProfile = await db.FreelancerProfiles.AnyAsync(fp => fp.UserId == currentUserId, ct);
        if (!hasProfile)
            return Result<ProposalDto>.BadRequest(ProposalTextResponses.NoFreelancerProfile).ToHttpResult();

        var alreadyApplied = await db.Proposals
            .AnyAsync(p => p.ProjectId == projectGuid && p.FreelancerId == currentUserId, ct);
        if (alreadyApplied)
            return Result<ProposalDto>.Conflict(ProposalTextResponses.AlreadyApplied).ToHttpResult();

        var proposal = new ProposalEntity
        {
            ProjectId = projectGuid,
            FreelancerId = currentUserId,
            Price = request.Price,
            EstimatedDays = request.EstimatedDays,
            CoverLetter = request.CoverLetter,
            Status = ProposalStatus.Pending
        };

        db.Proposals.Add(proposal);
        await db.SaveChangesAsync(ct);

        var reloaded = await db.Proposals
            .Include(p => p.Project)
            .Include(p => p.Freelancer)
            .FirstAsync(p => p.Id == proposal.Id, ct);

        return Result<ProposalDto>.Created(ProposalDto.FromEntity(reloaded), ProposalTextResponses.Submitted).ToHttpResult();
    }
}