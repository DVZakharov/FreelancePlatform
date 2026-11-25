using FreelancePlatform.Domain.Common;

namespace FreelancePlatform.Domain.Entities;

public class RefreshTokenEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    // Новые поля — метаданные сессии
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public uint Version { get; set; }
    
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
