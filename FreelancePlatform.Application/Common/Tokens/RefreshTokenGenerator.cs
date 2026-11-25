using FreelancePlatform.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace FreelancePlatform.Application.Common.Tokens;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}