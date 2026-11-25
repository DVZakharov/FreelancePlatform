using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.GetMyProfile;

public static class GetMyProfileEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/users/me/freelancer-profiles"), Tags("FreelancerProfiles")]
    [EndpointSummary("Получить свой профиль фрилансера")]
    [EndpointDescription("Возвращает профиль фрилансера текущего авторизованного пользователя. " +
        "Возвращает 404, если профиль ещё не создан.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var profile = await db.FreelancerProfiles
            .Include(p => p.User)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == currentUserId, ct);

        if (profile is null)
            return Result<FreelancerProfileDto>.NotFound(ProfileTextResponses.NotFound).ToHttpResult();

        return Result<FreelancerProfileDto>.Ok(FreelancerProfileDto.FromEntity(profile)).ToHttpResult();
    }
}