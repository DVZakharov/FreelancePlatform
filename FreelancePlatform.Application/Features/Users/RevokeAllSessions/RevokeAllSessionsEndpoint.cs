using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Users.RevokeAllSessions;

public static class RevokeAllSessionsEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/users/me/sessions/revoke-all"), Tags("Users")]
    [EndpointSummary("Отозвать все активные сессии пользователя")]
    [EndpointDescription("Этот эндпоинт позволяет пользователю отозвать все свои активные сессии, включая текущую. \n" +
        "После вызова этого эндпоинта, все токены обновления будут аннулированы, и пользователь будет вынужден повторно аутентифицироваться.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var activeSessions = await db.RefreshTokens
            .Where(r => r.UserId == currentUserId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in activeSessions)
            session.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(new { revokedCount = activeSessions.Count }).ToHttpResult();
    }
}