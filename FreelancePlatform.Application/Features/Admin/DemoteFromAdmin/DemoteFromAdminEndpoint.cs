using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FreelancePlatform.Application.Features.Admin.DemoteFromAdmin;

public static class DemoteFromAdminEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverinePost("/api/v1/admin/users/{id}/demote"), Tags("Admin")]
    [EndpointSummary("Снять роль администратора")]
    [EndpointDescription("Снимает роль Admin с указанного пользователя по email. Защищено секретным ключом " +
                         "настройки (не требует роли Admin, чтобы не создавать циклическую зависимость). " +
                         "Нельзя разжаловать последнего администратора в системе.")]
    public static async Task<IResult> Handle(
        DemoteFromAdminRequest request,
        IApplicationDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        var expectedSecret = config["Admin:SetupSecret"];
        if (string.IsNullOrEmpty(expectedSecret) || request.SetupSecret != expectedSecret)
            return Result<object>.Forbidden(AdminTextResponses.InvalidSecret).ToHttpResult();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null)
            return Result<object>.NotFound(AdminTextResponses.UserNotFound).ToHttpResult();

        if (!user.Role.HasFlag(UserRole.Admin))
            return Result<object>.Conflict(AdminTextResponses.NotAnAdmin).ToHttpResult();

        var adminCount = await db.Users.CountAsync(u => (u.Role & UserRole.Admin) == UserRole.Admin, ct);
        if (adminCount <= 1)
            return Result<object>.Conflict(AdminTextResponses.CannotDemoteLastAdmin).ToHttpResult();

        user.Role &= ~UserRole.Admin;
        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, AdminTextResponses.Demoted).ToHttpResult();
    }
}