using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Milestones.ApproveMilestone;

public static class ApproveMilestoneEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/milestones/{id}/approve"), Tags("Milestones")]
    [EndpointSummary("Принятие этапа")]
    [EndpointDescription("Заказчик принимает выполненную работу. Замороженные средства переводятся " +
        "на баланс фрилансера, этап переходит в статус Completed. " +
        "Доступно только заказчику контракта, только для этапа в статусе Submitted.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<ApproveMilestoneRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new ApproveMilestoneRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var milestone = await db.Milestones
            .Include(m => m.Contract)
            .Include(m => m.EscrowHold)
            .FirstOrDefaultAsync(m => m.Id == Guid.Parse(id), ct);

        if (milestone is null)
            return Result<MilestoneDto>.NotFound(MilestoneTextResponses.NotFound).ToHttpResult();

        if (milestone.Contract.ClientId != currentUserId)
            return Result<MilestoneDto>.Forbidden(MilestoneTextResponses.NotClient).ToHttpResult();

        if (milestone.Contract.Status != ContractStatus.Active) 
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.ContractNotActive).ToHttpResult(); 
        
        if (milestone.Status != MilestoneStatus.Submitted)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.NotSubmitted).ToHttpResult();

        var freelancer = await db.Users.FindAsync([milestone.Contract.FreelancerId], ct);

        milestone.EscrowHold!.Status = HoldStatus.Released;
        milestone.EscrowHold.ResolvedAt = DateTime.UtcNow;
        milestone.Status = MilestoneStatus.Completed;

        freelancer!.Balance += milestone.Amount;

        db.Transactions.Add(new TransactionEntity
        {
            UserId = freelancer.Id,
            ContractId = milestone.ContractId,
            MilestoneId = milestone.Id,
            Type = TransactionType.Release,
            Amount = milestone.Amount,
            BalanceAfter = freelancer.Balance,
            Description = $"Released for milestone: {milestone.Title}"
        });

        await db.SaveChangesAsync(ct);

        return Result<MilestoneDto>.Ok(MilestoneDto.FromEntity(milestone), MilestoneTextResponses.Approved).ToHttpResult();
    }
}