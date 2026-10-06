using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Milestones.SubmitMilestone;

public static class SubmitMilestoneEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/milestones/{id}/submit"), Tags("Milestones")]
    [EndpointSummary("Сдача этапа")]
    [EndpointDescription("Фрилансер отмечает этап как выполненный и готовый к проверке заказчиком. " +
                         "Доступно только фрилансеру контракта, только для этапа в статусе Funded.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<SubmitMilestoneRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new SubmitMilestoneRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var milestone = await db.Milestones
            .Include(m => m.Contract)
            .FirstOrDefaultAsync(m => m.Id == Guid.Parse(id), ct);

        if (milestone is null)
            return Result<MilestoneDto>.NotFound(MilestoneTextResponses.NotFound).ToHttpResult();

        if (milestone.Contract.FreelancerId != currentUserId)
            return Result<MilestoneDto>.Forbidden(MilestoneTextResponses.NotFreelancer).ToHttpResult();

        if (milestone.Status != MilestoneStatus.Funded)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.NotFunded).ToHttpResult();

        milestone.Status = MilestoneStatus.Submitted;

        await db.SaveChangesAsync(ct);

        return Result<MilestoneDto>.Ok(MilestoneDto.FromEntity(milestone), MilestoneTextResponses.Submitted).ToHttpResult();
    }
}