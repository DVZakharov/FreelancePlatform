using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Milestones.GetMilestonesForContract;

public static class GetMilestonesForContractEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/contracts/{contractId}/milestones"), Tags("Milestones")]
    [EndpointSummary("Этапы контракта")]
    [EndpointDescription("Возвращает все этапы указанного контракта. Доступно обеим сторонам контракта — заказчику и фрилансеру.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        IValidator<GetMilestonesForContractRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetMilestonesForContractRequest(contractId);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<List<MilestoneDto>>.BadRequest(MilestoneTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == Guid.Parse(contractId), ct);
        if (contract is null)
            return Result<List<MilestoneDto>>.NotFound(MilestoneTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<List<MilestoneDto>>.Forbidden(MilestoneTextResponses.NotParticipant).ToHttpResult();

        var milestones = await db.Milestones
            .Where(m => m.ContractId == Guid.Parse(contractId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var dtos = milestones.Select(MilestoneDto.FromEntity).ToList();

        return Result<List<MilestoneDto>>.Ok(dtos, MilestoneTextResponses.GetSuccess).ToHttpResult();
    }
}