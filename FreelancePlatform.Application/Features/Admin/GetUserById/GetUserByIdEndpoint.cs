using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Admin.GetUserById;

public static class GetUserByIdEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverineGet("/api/v1/admin/users/{id}"), Tags("Admin")]
    [EndpointSummary("Детали пользователя")]
    [EndpointDescription("Возвращает информацию о пользователе и сводные счётчики его активности: проекты, " +
        "отклики, контракты, активные сессии, транзакции. Доступно только администраторам.")]
    public static async Task<IResult> Handle(
        string id,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var userId))
            return Result<AdminUserDetailsDto>.BadRequest(AdminTextResponses.ValidationError, ["Invalid user id"]).ToHttpResult();

        var user = await db.Users
            .Include(u => u.FreelancerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<AdminUserDetailsDto>.NotFound(AdminTextResponses.UserNotFound).ToHttpResult();

        var now = DateTime.UtcNow;

        var projectsCount = await db.Projects.CountAsync(p => p.ClientId == userId, ct);
        var proposalsCount = await db.Proposals.CountAsync(p => p.FreelancerId == userId, ct);
        var contractsCount = await db.Contracts.CountAsync(c => c.ClientId == userId || c.FreelancerId == userId, ct);
        var sessionsCount = await db.RefreshTokens.CountAsync(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > now, ct);
        var transactionsCount = await db.Transactions.CountAsync(t => t.UserId == userId, ct);

        var dto = new AdminUserDetailsDto(
            UserDto.FromEntity(user, user.FreelancerProfile is not null),
            projectsCount,
            proposalsCount,
            contractsCount,
            sessionsCount,
            transactionsCount);

        return Result<AdminUserDetailsDto>.Ok(dto, AdminTextResponses.UserRetrieved).ToHttpResult();
    }
}