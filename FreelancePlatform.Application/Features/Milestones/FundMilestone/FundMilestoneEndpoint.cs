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

namespace FreelancePlatform.Application.Features.Milestones.FundMilestone;

public static class FundMilestoneEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/milestones/{id}/fund"), Tags("Milestones")]
    [EndpointSummary("Финансирование этапа (эскроу)")]
    [EndpointDescription("Заказчик замораживает средства на сумму этапа в эскроу. " +
        "Сумма списывается с баланса заказчика и хранится до момента одобрения или спора. " +
        "Доступно только заказчику контракта, только для этапа в статусе Pending.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<FundMilestoneRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new FundMilestoneRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var milestone = await db.Milestones
            .Include(m => m.Contract)
            .FirstOrDefaultAsync(m => m.Id == Guid.Parse(id), ct);

        if (milestone is null)
            return Result<MilestoneDto>.NotFound(MilestoneTextResponses.NotFound).ToHttpResult();

        if (milestone.Contract.ClientId != currentUserId)
            return Result<MilestoneDto>.Forbidden(MilestoneTextResponses.NotClient).ToHttpResult();

        if (milestone.Contract.Status != ContractStatus.Active)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.ContractNotActive).ToHttpResult();

        if (milestone.Status != MilestoneStatus.Pending)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.AlreadyFunded).ToHttpResult();

        var client = await db.Users.FindAsync([currentUserId], ct);
        if (client!.Balance < milestone.Amount)
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.InsufficientBalance).ToHttpResult();

        client.Balance -= milestone.Amount;
        milestone.Status = MilestoneStatus.Funded;

        db.EscrowHolds.Add(new EscrowHoldEntity
        {
            MilestoneId = milestone.Id,
            ContractId = milestone.ContractId,
            Amount = milestone.Amount,
            Status = HoldStatus.Held
        });

        db.Transactions.Add(new TransactionEntity
        {
            UserId = client.Id,
            ContractId = milestone.ContractId,
            MilestoneId = milestone.Id,
            Type = TransactionType.Freeze,
            Amount = milestone.Amount,
            BalanceAfter = client.Balance,
            Description = $"Funded milestone: {milestone.Title}"
        });

        await db.SaveChangesAsync(ct);

        return Result<MilestoneDto>.Ok(MilestoneDto.FromEntity(milestone), MilestoneTextResponses.Funded).ToHttpResult();
    }
}