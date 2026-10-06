using System.Data.Entity;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Users.GetActiveSessions;

public static class GetActiveSessionsEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/users/me/sessions"), Tags("Users")]
    [EndpointSummary("Получить активные сессии пользователя")]
    [EndpointDescription("Возвращает список активных сессий текущего пользователя.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();
        var now = DateTime.UtcNow;

        var sessions = await db.RefreshTokens
            .Where(r => r.UserId == currentUserId && r.RevokedAt == null && r.ExpiresAt > now)
            .OrderByDescending(r => r.LastUsedAt ?? r.CreatedAt)
            .Select(r => SessionDto.FromEntity(r))
            .ToListAsync(ct);

        var listSessionsDto = new ListSessionsDto
        {
            Sessions = sessions,
            Count = sessions.Count
        };

        return Result<ListSessionsDto>.Ok(listSessionsDto, UserTextResponses.GetActiveSessionsSuccess).ToHttpResult();
    }
}