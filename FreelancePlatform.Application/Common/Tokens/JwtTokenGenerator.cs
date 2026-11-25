using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreelancePlatform.Domain.Enums;

namespace FreelancePlatform.Application.Common.Tokens;

public class JwtTokenGenerator(IOptions<JwtSettings> settings) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = settings.Value;

    public string GenerateToken(UserEntity user)
    {
        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(Enum.GetValues<UserRole>().Where(r => r != UserRole.None && user.Role.HasFlag(r)).Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}