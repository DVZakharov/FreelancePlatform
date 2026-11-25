using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Auth.Logout;

public static class LogoutEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/auth/logout"), Tags("Authentication")]
    [EndpointSummary("Выйти из системы")]
    [EndpointDescription("Отозвать токен обновления и выйти из системы")]
    public static async Task<IResult> Handle(
        [FromBody] LogoutRequest request,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Result<object>.Ok(AuthTextResponses.LogoutSuccessful).ToHttpResult();
    }
}