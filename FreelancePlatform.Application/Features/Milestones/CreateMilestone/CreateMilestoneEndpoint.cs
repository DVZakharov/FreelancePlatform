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

namespace FreelancePlatform.Application.Features.Milestones.CreateMilestone;

public static class CreateMilestoneEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/contracts/{contractId}/milestones"), Tags("Milestones")]
    [EndpointSummary("Создание этапа контракта")]
    [EndpointDescription("Создаёт новый этап (milestone) в рамках активного контракта с указанной суммой. " +
        "Доступно только заказчику. Этап создаётся в статусе Pending и требует последующего финансирования.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        [FromBody] CreateMilestoneRequest request,
        HttpContext ctx,
        IValidator<CreateMilestoneRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MilestoneDto>.BadRequest(
                MilestoneTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(contractId, out var contractGuid))
            return Result<MilestoneDto>.BadRequest(MilestoneTextResponses.ValidationError, [MilestoneTextResponses.InvalidContractId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractGuid, ct);
        if (contract is null)
            return Result<MilestoneDto>.NotFound(MilestoneTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId)
            return Result<MilestoneDto>.Forbidden(MilestoneTextResponses.NotClient).ToHttpResult();

        if (contract.Status != ContractStatus.Active)
            return Result<MilestoneDto>.Conflict(MilestoneTextResponses.ContractNotActive).ToHttpResult();

        var milestone = new MilestoneEntity
        {
            ContractId = contractGuid,
            Title = request.Title,
            Amount = request.Amount,
            Status = MilestoneStatus.Pending,
            DueDate = request.DueDate
        };

        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(ct);

        return Result<MilestoneDto>.Created(MilestoneDto.FromEntity(milestone), MilestoneTextResponses.Created).ToHttpResult();
    }
}