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

namespace FreelancePlatform.Application.Features.Disputes.OpenDispute;

public static class OpenDisputeEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/contracts/{contractId}/disputes"), Tags("Disputes")]
    [EndpointSummary("Открыть спор")]
    [EndpointDescription("Открывает спор по контракту. Доступно любой из сторон контракта (заказчику или " +
        "фрилансеру), только пока контракт активен. Контракт переходит в статус InDispute, " +
        "новые действия по этапам блокируются до разрешения спора администратором.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        [FromBody] OpenDisputeRequest request,
        HttpContext ctx,
        IValidator<OpenDisputeRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<DisputeDto>.BadRequest(
                DisputeTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(contractId, out var contractGuid))
            return Result<DisputeDto>.BadRequest(DisputeTextResponses.ValidationError, [DisputeTextResponses.InvalidContractId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractGuid, ct);
        if (contract is null)
            return Result<DisputeDto>.NotFound(DisputeTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<DisputeDto>.Forbidden(DisputeTextResponses.NotParticipant).ToHttpResult();

        if (contract.Status != ContractStatus.Active)
            return Result<DisputeDto>.Conflict(DisputeTextResponses.ContractNotActive).ToHttpResult();

        var dispute = new DisputeEntity
        {
            ContractId = contractGuid,
            OpenedById = currentUserId,
            Reason = request.Reason,
            Status = DisputeStatus.Open
        };

        db.Disputes.Add(dispute);
        contract.Status = ContractStatus.InDispute;

        await db.SaveChangesAsync(ct);

        return Result<DisputeDto>.Created(DisputeDto.FromEntity(dispute), DisputeTextResponses.Opened).ToHttpResult();
    }
}