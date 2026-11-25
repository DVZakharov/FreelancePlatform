using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Payments.GetMyBalance;

public static class GetMyBalanceEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/payments/balance"), Tags("Payments")]
    [EndpointSummary("Текущий баланс")]
    [EndpointDescription("Возвращает текущий баланс авторизованного пользователя.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var user = await db.Users.FindAsync([currentUserId], ct);

        return Result<GetMyBalanceResponse>.Ok(
            new GetMyBalanceResponse(user!.Balance),
            PaymentTextResponses.BalanceRetrieved).ToHttpResult();
    }
}