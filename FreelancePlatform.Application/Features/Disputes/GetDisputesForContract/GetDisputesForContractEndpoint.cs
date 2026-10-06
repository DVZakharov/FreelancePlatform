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

namespace FreelancePlatform.Application.Features.Disputes.GetDisputesForContract;

public static class GetDisputesForContractEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/contracts/{contractId}/disputes"), Tags("Disputes")]
    [EndpointSummary("Споры по контракту")]
    [EndpointDescription("Возвращает все споры (включая уже разрешённые) по указанному контракту. Доступно участникам контракта.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        IValidator<GetDisputesForContractRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetDisputesForContractRequest(contractId);
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<List<DisputeDto>>.BadRequest(DisputeTextResponses.ValidationError, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == Guid.Parse(contractId), ct);
        if (contract is null)
            return Result<List<DisputeDto>>.NotFound(DisputeTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<List<DisputeDto>>.Forbidden(DisputeTextResponses.NotParticipant).ToHttpResult();

        var disputes = await db.Disputes
            .Where(d => d.ContractId == Guid.Parse(contractId))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var dtos = disputes.Select(DisputeDto.FromEntity).ToList();

        return Result<List<DisputeDto>>.Ok(dtos, DisputeTextResponses.GetSuccess).ToHttpResult();
    }
}