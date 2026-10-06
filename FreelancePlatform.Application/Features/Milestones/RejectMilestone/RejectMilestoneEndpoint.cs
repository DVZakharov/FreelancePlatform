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

namespace FreelancePlatform.Application.Features.Milestones.RejectMilestone;

public static class RejectMilestoneEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/milestones/{id}/reject"), Tags("Milestones")]
    [EndpointSummary("Отклонение этапа")]
    [EndpointDescription("Заказчик отправляет выполненную работу на доработку. Замороженные средства " +
        "остаются в эскроу (не возвращаются), этап переходит обратно в статус Funded для повторной сдачи. " +
        "Доступно только заказчику контракта, только для этапа в статусе Submitted.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] RejectMilestoneRequest request,
        HttpContext ctx,
        IValidator<RejectMilestoneRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MilestoneDto>.BadRequest(
                MilestoneTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(id, out var milestoneId))
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.ValidationError, [MilestoneTextResponses.InvalidId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var milestone = await db.Milestones
            .Include(m => m.Contract)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, ct);

        if (milestone is null)
            return Result<MilestoneDto>.NotFound(MilestoneTextResponses.NotFound).ToHttpResult();

        if (milestone.Contract.ClientId != currentUserId)
            return Result<MilestoneDto>.Forbidden(MilestoneTextResponses.NotClient).ToHttpResult();

        if (milestone.Status != MilestoneStatus.Submitted)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.NotSubmitted).ToHttpResult();

        milestone.Status = MilestoneStatus.Funded;   // возврат к Funded, деньги остаются в эскроу

        await db.SaveChangesAsync(ct);

        return Result<MilestoneDto>.Ok(MilestoneDto.FromEntity(milestone), MilestoneTextResponses.Rejected).ToHttpResult();
    }
}