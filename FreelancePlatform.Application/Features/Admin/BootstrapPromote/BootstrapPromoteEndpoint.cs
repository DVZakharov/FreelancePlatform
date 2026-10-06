using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FreelancePlatform.Application.Features.Admin.BootstrapPromote;

public static class BootstrapPromoteEndpoint
{
    [WolverinePost("/api/v1/admin/bootstrap/promote"), Tags("Admin")]
    [EndpointSummary("Назначить первого администратора (bootstrap)")]
    [EndpointDescription("Назначает роль Admin указанному пользователю по email. Работает только если в " +
        "системе ещё нет ни одного администратора — используется один раз при разворачивании платформы. " +
        "Защищён секретным ключом из конфигурации (Admin:SetupSecret), а не ролью, так как на этом этапе " +
        "ролевая проверка ещё невозможна (admin'ов ещё нет).")]
    public static async Task<IResult> Handle(
        BootstrapPromoteRequest request,
        IValidator<BootstrapPromoteRequest> validator,
        IApplicationDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<object>.BadRequest(
                AdminTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var hasAdmin = await db.Users.AnyAsync(u => (u.Role & UserRole.Admin) == UserRole.Admin, ct);
        if (hasAdmin)
            return Result<object>.Forbidden(AdminTextResponses.AlreadyAdmin).ToHttpResult();

        var expectedSecret = config["Admin:SetupSecret"];
        if (string.IsNullOrEmpty(expectedSecret) || request.SetupSecret != expectedSecret)
            return Result<object>.Forbidden(AdminTextResponses.InvalidSecret).ToHttpResult();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null)
            return Result<object>.NotFound(AdminTextResponses.UserNotFound).ToHttpResult();

        user.Role |= UserRole.Admin;
        await db.SaveChangesAsync(ct);

        return Result<object>.Ok(null, AdminTextResponses.Promoted).ToHttpResult();
    }
}