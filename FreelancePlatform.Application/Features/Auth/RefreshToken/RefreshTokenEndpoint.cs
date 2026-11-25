using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.Common.Tokens;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Auth.RefreshToken;

public static class RefreshTokenEndpoint
{
    [WolverinePost("/api/v1/auth/refresh"), Tags("Authentication")]
    [EndpointSummary("Обновление токена доступа с помощью refresh-токена")]
    [EndpointDescription("Обновляет токен доступа, используя действительный refresh-токен. " +
        "Выданный токен одноразовый: при обновлении он заменяется новым. " +
        "Повторное использование уже отозванного токена считается признаком кражи, " +
        "и все активные сессии пользователя отзываются.")]
    public static async Task<IResult> Handle(
        [FromBody] RefreshTokenRequest request,
        IApplicationDbContext db,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenGenerator refreshGenerator,
        JwtSettings jwtSettings,
        CancellationToken ct)
    {
        var existingToken = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (existingToken is null)
            return Result<LoginResponse>.Unauthorized(AuthTextResponses.InvalidRefreshToken).ToHttpResult();

        // Токен уже был использован или отозван: возможная кража.
        // Отзываем ВСЕ активные токены пользователя.
        if (existingToken.RevokedAt is not null)
        {
            var activeTokens = await db.RefreshTokens
                .Where(r => r.UserId == existingToken.UserId && r.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
                token.RevokedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Result<LoginResponse>.Unauthorized(AuthTextResponses.RefreshTokenRevoked).ToHttpResult();
        }

        // Просто истёк срок: ничего не отзываем, другие сессии пользователя не трогаем
        if (existingToken.ExpiresAt <= DateTime.UtcNow)
            return Result<LoginResponse>.Unauthorized(AuthTextResponses.RefreshTokenExpired).ToHttpResult();

        // Ротация: старый токен отзываем, выдаём новую пару
        var newRefreshToken = refreshGenerator.GenerateToken();

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken;

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = existingToken.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays)
        });

        var newAccessToken = jwtGenerator.GenerateToken(existingToken.User);

        await db.SaveChangesAsync(ct);

        return Result<LoginResponse>.Ok(
            new LoginResponse(
                newAccessToken,
                newRefreshToken,
                existingToken.User.Id,
                existingToken.User.Email,
                existingToken.User.FullName),
            AuthTextResponses.RefreshTokenSuccessful).ToHttpResult();
    }
}