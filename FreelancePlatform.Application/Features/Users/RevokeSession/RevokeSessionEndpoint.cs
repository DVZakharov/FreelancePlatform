using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Users.RevokeSession;

public static class RevokeSessionEndpoint
{
    [Authorize]
    [WolverineDelete("/api/v1/users/me/sessions/{id}"), Tags("Users")]
    [EndpointSummary("Отозвать сессию пользователя")]
    [EndpointDescription("Отзывает указанную сессию пользователя.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var sessionId))
            return Result<object>.BadRequest(UserTextResponses.ValidationError, ["Invalid session id"]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var session = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Id == sessionId, ct);
        if (session is null)
            return Result<object>.NotFound(UserTextResponses.SessionNotFound).ToHttpResult();

        if (session.UserId != currentUserId)
            return Result<object>.Forbidden(UserTextResponses.NotOwnSession).ToHttpResult();

        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Result<object>.Ok(null, UserTextResponses.SessionRevoked).ToHttpResult();
    }
}