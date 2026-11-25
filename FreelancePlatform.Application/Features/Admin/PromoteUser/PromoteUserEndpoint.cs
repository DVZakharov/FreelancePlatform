using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Admin.PromoteUser;

public static class PromoteUserEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverinePost("/api/v1/admin/users/{id}/promote"), Tags("Admin")]
    [EndpointSummary("Назначить пользователя администратором")]
    [EndpointDescription("Назначает роль Admin указанному пользователю. Доступно только действующим администраторам.")]
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

        if (user.Role.HasFlag(UserRole.Admin))
            return Result<object>.Conflict(AdminTextResponses.AlreadyAdmin).ToHttpResult();

        user.Role |= UserRole.Admin;
        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, AdminTextResponses.Promoted).ToHttpResult();
    }
}