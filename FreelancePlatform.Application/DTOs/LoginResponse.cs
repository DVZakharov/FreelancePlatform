namespace FreelancePlatform.Application.DTOs;

public sealed record LoginResponse(string AccessToken, string RefreshToken, Guid UserId, string Email, string FullName);