using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Auth.Login;

public static class LoginEndpoint
{
    [WolverinePost("/api/v1/auth/login"), Tags("Authentication")]
    [EndpointSummary("Аутентификация пользователя")]
    [EndpointDescription("Возвращает токены доступа и обновления, а также информацию о пользователе.")]
    public static async Task<IResult> Handle(
        [FromBody] LoginRequest request,
        HttpContext ctx,
        IApplicationDbContext db,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Unauthorized(AuthTextResponses.InvalidCredentials).ToHttpResult();

        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();

        var (accessToken, refreshToken) = await tokenService.IssueTokensAsync(user, userAgent, ipAddress, ct);

        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, refreshToken, user.Id, user.Email, user.FullName), AuthTextResponses.LoginSuccessful).ToHttpResult();
    }
}
