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

namespace FreelancePlatform.Application.Features.Proposals.AcceptProposal;

public static class AcceptProposalEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/proposals/{id}/accept"), Tags("Proposals")]
    [EndpointSummary("Принять отклик")]
    [EndpointDescription("Заказчик принимает отклик фрилансера. Проект переходит в статус InProgress, " +
        "все остальные отклики на этот проект автоматически отклоняются, создаётся контракт " +
        "с зафиксированными условиями (цена и срок из отклика).")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<AcceptProposalRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new AcceptProposalRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ContractDto>.BadRequest(
                ProposalTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var proposal = await db.Proposals
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id), ct);

        if (proposal is null)
            return Result<ContractDto>.NotFound(ProposalTextResponses.NotFound).ToHttpResult();

        if (proposal.Project.ClientId != currentUserId)
            return Result<ContractDto>.Forbidden(ProposalTextResponses.NotOwnerOfProject).ToHttpResult();

        if (proposal.Project.Status != ProjectStatus.Open)
            return Result<ContractDto>.Conflict(ProposalTextResponses.ProjectNotOpen).ToHttpResult();

        if (proposal.Status != ProposalStatus.Pending)
            return Result<ContractDto>.Conflict(ProposalTextResponses.NotPending).ToHttpResult();

        // Все остальные отклики на этот проект — отклонить
        var otherProposals = await db.Proposals
            .Where(p => p.ProjectId == proposal.ProjectId && p.Id != proposal.Id && p.Status == ProposalStatus.Pending)
            .ToListAsync(ct);

        foreach (var other in otherProposals)
            other.Status = ProposalStatus.Rejected;

        proposal.Status = ProposalStatus.Accepted;

        proposal.Project.Status = ProjectStatus.InProgress;
        proposal.Project.SelectedProposalId = proposal.Id;

        var contract = new ContractEntity
        {
            ProjectId = proposal.ProjectId,
            ClientId = proposal.Project.ClientId,
            FreelancerId = proposal.FreelancerId,
            AgreedPrice = proposal.Price,
            AgreedDeadline = DateTime.UtcNow.AddDays(proposal.EstimatedDays),
            Status = ContractStatus.Active
        };

        db.Contracts.Add(contract);

        await db.SaveChangesAsync(ct);

        var reloaded = await db.Contracts
            .Include(c => c.Project)
            .Include(c => c.Client)
            .Include(c => c.Freelancer)
            .FirstAsync(c => c.Id == contract.Id, ct);

        return Result<ContractDto>.Created(ContractDto.FromEntity(reloaded), ProposalTextResponses.Accepted).ToHttpResult();
    }
}
