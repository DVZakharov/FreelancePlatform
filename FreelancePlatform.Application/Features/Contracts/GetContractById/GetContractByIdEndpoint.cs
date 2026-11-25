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

namespace FreelancePlatform.Application.Features.Contracts.GetContractById;

public static class GetContractByIdEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/contracts/{id}"), Tags("Contracts")]
    [EndpointSummary("Получить контракт по ID")]
    [EndpointDescription("Возвращает детали контракта. Доступно только участникам контракта — заказчику и фрилансеру.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetContractByIdRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetContractByIdRequest(id);
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<ContractDto>.BadRequest(ContractTextResponses.ValidationError, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts
            .Include(c => c.Project)
            .Include(c => c.Client)
            .Include(c => c.Freelancer)
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(id), ct);

        if (contract is null)
            return Result<ContractDto>.NotFound(ContractTextResponses.NotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<ContractDto>.Forbidden(ContractTextResponses.NotParticipant).ToHttpResult();

        return Result<ContractDto>.Ok(ContractDto.FromEntity(contract), ContractTextResponses.GetSuccess).ToHttpResult();
    }
}