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
    [EndpointDescription("Обновляет токен доступа, используя действительный refresh-токен.")]
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
            return Result<LoginResponse>.BadRequest(AuthTextResponses.InvalidRefreshToken).ToHttpResult();

        // Токен уже был отозван/использован — возможная компрометация.
        // Отзываем ВСЕ активные токены пользователя ради безопасности.
        if (!existingToken.IsActive)
        {
            var allUserTokens = await db.RefreshTokens
                .Where(r => r.UserId == existingToken.UserId && r.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in allUserTokens)
                t.RevokedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Result<LoginResponse>.BadRequest(AuthTextResponses.RefreshTokenExpired).ToHttpResult();
        }

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

        return Result<LoginResponse>.Ok(new LoginResponse(
            newAccessToken, newRefreshToken, existingToken.User.Id, existingToken.User.Email, existingToken.User.FullName), AuthTextResponses.RefreshTokenSuccessful).ToHttpResult();
    }
}