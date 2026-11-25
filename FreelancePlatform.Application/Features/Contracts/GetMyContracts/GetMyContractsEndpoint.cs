using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Contracts.GetMyContracts;

public static class GetMyContractsEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/contracts/me"), Tags("Contracts")]
    [EndpointSummary("Мои контракты")]
    [EndpointDescription("Возвращает все контракты текущего пользователя — как заказчика, так и фрилансера, " +
                         "с пометкой роли в каждом конкретном контракте.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var contracts = await db.Contracts
            .Include(c => c.Project)
            .Include(c => c.Client)
            .Include(c => c.Freelancer)
            .Where(c => c.ClientId == currentUserId || c.FreelancerId == currentUserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var items = contracts.Select(c => new MyContractDto(
            ContractDto.FromEntity(c),
            c.ClientId == currentUserId ? "Client" : "Freelancer")).ToList();

        return Result<GetMyContractsResponse>.Ok(
            new GetMyContractsResponse(items),
            ContractTextResponses.GetSuccess).ToHttpResult();
    }
}