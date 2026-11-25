using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FreelancePlatform.Application.Common.Tokens;

public class TokenService : ITokenService
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenGenerator _refreshGenerator;
    private readonly JwtSettings _settings;

    public TokenService(
        IApplicationDbContext db,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenGenerator refreshGenerator,
        IOptions<JwtSettings> settings)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
        _refreshGenerator = refreshGenerator;
        _settings = settings.Value;
    }
    public async Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(
        UserEntity user, string? userAgent, string? ipAddress, CancellationToken ct)
    {
        var accessToken = _jwtGenerator.GenerateToken(user);
        var refreshToken = _refreshGenerator.GenerateToken();

        _db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays),
            UserAgent = userAgent,
            IpAddress = ipAddress,
            LastUsedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return (accessToken, refreshToken);
    }
}