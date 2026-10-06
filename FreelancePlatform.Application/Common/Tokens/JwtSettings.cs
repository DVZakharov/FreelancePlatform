namespace FreelancePlatform.Application.Common.Tokens;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 30;
    public int RefreshTokenExpiryDays { get; set; } = 14;
}
