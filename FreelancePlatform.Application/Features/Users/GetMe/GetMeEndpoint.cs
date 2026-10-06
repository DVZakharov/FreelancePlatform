using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Users.GetMe;

public static class GetMeEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/users/me"), Tags("Users")]
    [EndpointSummary("Текущий пользователь")]
    [EndpointDescription("Возвращает базовую информацию о текущем авторизованном пользователе: " +
                         "email, имя, роли, баланс, наличие профиля фрилансера.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var user = await db.Users.FindAsync([currentUserId], ct);
        if (user is null)
            return Result<UserDto>.NotFound(UserTextResponses.NotFound).ToHttpResult();

        var isFreelancer = await db.FreelancerProfiles.AnyAsync(fp => fp.UserId == currentUserId, ct);

        return Result<UserDto>.Ok(UserDto.FromEntity(user, isFreelancer), UserTextResponses.GetSuccess).ToHttpResult();
    }
}