using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Auth.Register;

public static class RegisterEndpoint
{
    [WolverinePost("/api/v1/auth/register"), Tags("Authentication")]
    [EndpointSummary("Регистрация нового пользователя")]
    [EndpointDescription("Возвращает токены доступа и обновления, а также информацию о зарегистрированном пользователе.")]
    public static async Task<IResult> Handle(
        [FromBody] RegisterRequest request,
        IApplicationDbContext db,
        ITokenService tokenService,
        HttpContext ctx,
        IValidator<RegisterRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<LoginResponse>.BadRequest(AuthTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
            return Result<LoginResponse>.Conflict(AuthTextResponses.EmailAlreadyExists).ToHttpResult();

        var user = new UserEntity
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Client,
            Balance = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();

        var (accessToken, refreshToken) = await tokenService.IssueTokensAsync(user, userAgent, ipAddress, ct);

        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, refreshToken, user.Id, user.Email, user.FullName), AuthTextResponses.RegistrationSuccessful).ToHttpResult();
    }
}
