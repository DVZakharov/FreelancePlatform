using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Admin.DemoteUser;

public static class DemoteUserEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverinePost("/api/v1/admin/users/{id}/demote"), Tags("Admin")]
    [EndpointSummary("Снять роль администратора")]
    [EndpointDescription("Снимает роль Admin с указанного пользователя и отзывает все его активные сессии. " +
        "Нельзя разжаловать последнего администратора в системе. Доступно только действующим администраторам.")]
    public static async Task<IResult> Handle(
        string id,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var userId))
            return Result<object>.BadRequest(AdminTextResponses.ValidationError, [AdminTextResponses.InvalidUserId]).ToHttpResult();

        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<object>.NotFound(AdminTextResponses.UserNotFound).ToHttpResult();

        if (!user.Role.HasFlag(UserRole.Admin))
            return Result<object>.Conflict(AdminTextResponses.NotAnAdmin).ToHttpResult();

        var adminCount = await db.Users.CountAsync(u => (u.Role & UserRole.Admin) == UserRole.Admin, ct);
        if (adminCount <= 1)
            return Result<object>.Conflict(AdminTextResponses.CannotDemoteLastAdmin).ToHttpResult();

        user.Role &= ~UserRole.Admin;

        var sessions = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, AdminTextResponses.Demoted).ToHttpResult();
    }
}