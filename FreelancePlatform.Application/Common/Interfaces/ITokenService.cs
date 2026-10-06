using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.Common.Interfaces;

public interface ITokenService
{
    public Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(
        UserEntity user, string? userAgent, string? ipAddress, CancellationToken ct);
}