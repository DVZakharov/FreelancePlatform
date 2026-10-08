// ResolveDisputeEndpoint.cs
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

namespace FreelancePlatform.Application.Features.Disputes.ResolveDispute;

public static class ResolveDisputeEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverinePost("/api/v1/disputes/{id}/resolve"), Tags("Disputes")]
    [EndpointSummary("Разрешить спор")]
    [EndpointDescription("Администратор выносит решение по спору в пользу клиента или фрилансера. " +
        "Обрабатываются все текущие замороженные (Held) эскроу-холды контракта: при решении в пользу " +
        "клиента средства возвращаются ему, в пользу фрилансера — переводятся фрилансеру. " +
        "Контракт возвращается в статус Active.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] ResolveDisputeRequest request,
        HttpContext ctx,
        IValidator<ResolveDisputeRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<DisputeDto>.BadRequest(
                DisputeTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(id, out var disputeId))
            return Result<DisputeDto>.BadRequest(DisputeTextResponses.ValidationError, [DisputeTextResponses.InvalidId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();
        
        var dispute = await db.Disputes
            .Include(d => d.Contract)
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct);

        if (dispute is null)
            return Result<DisputeDto>.NotFound(DisputeTextResponses.NotFound).ToHttpResult();

        if (dispute.Status != DisputeStatus.Open)
            return Result<DisputeDto>.Conflict(DisputeTextResponses.NotOpen).ToHttpResult();

        var heldEscrows = await db.EscrowHolds
            .Include(e => e.Milestone)
            .Where(e => e.ContractId == dispute.ContractId && e.Status == HoldStatus.Held)
            .ToListAsync(ct);

        var client = await db.Users.FindAsync([dispute.Contract.ClientId], ct);
        var freelancer = await db.Users.FindAsync([dispute.Contract.FreelancerId], ct);

        foreach (var hold in heldEscrows)
        {
            if (request.Decision == "Client")
            {
                client!.Balance += hold.Amount;
                hold.Status = HoldStatus.Refunded;

                db.Transactions.Add(new TransactionEntity
                {
                    UserId = client.Id,
                    ContractId = dispute.ContractId,
                    MilestoneId = hold.MilestoneId,
                    Type = TransactionType.Refund,
                    Amount = hold.Amount,
                    BalanceAfter = client.Balance,
                    Description = $"Dispute resolved in favor of client: {hold.Milestone.Title}"
                });
            }
            else // "Freelancer"
            {
                freelancer!.Balance += hold.Amount;
                hold.Status = HoldStatus.Released;
                hold.Milestone.Status = MilestoneStatus.Completed;

                db.Transactions.Add(new TransactionEntity
                {
                    UserId = freelancer.Id,
                    ContractId = dispute.ContractId,
                    MilestoneId = hold.MilestoneId,
                    Type = TransactionType.Release,
                    Amount = hold.Amount,
                    BalanceAfter = freelancer.Balance,
                    Description = $"Dispute resolved in favor of freelancer: {hold.Milestone.Title}"
                });
            }

            hold.ResolvedAt = DateTime.UtcNow;
        }

        dispute.Status = request.Decision == "Client"
            ? DisputeStatus.ResolvedForClient
            : DisputeStatus.ResolvedForFreelancer;
        dispute.Resolution = request.Resolution;
        dispute.ResolvedById = currentUserId;
        dispute.ResolvedAt = DateTime.UtcNow;

        dispute.Contract.Status = ContractStatus.Active;

        await db.SaveChangesAsync(ct);

        return Result<DisputeDto>.Ok(DisputeDto.FromEntity(dispute), DisputeTextResponses.Resolved).ToHttpResult();
    }
}