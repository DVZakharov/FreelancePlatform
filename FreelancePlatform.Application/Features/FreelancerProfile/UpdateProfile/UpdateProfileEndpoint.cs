using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.FreelancerProfile.UpdateProfile;

public static class UpdateProfileEndpoint
{
    [Authorize]
    [WolverinePut("/api/v1/users/me/freelancer-profiles"), Tags("FreelancerProfiles")]
    [EndpointSummary("Обновить свой профиль фрилансера")]
    [EndpointDescription("Обновляет Bio, почасовую ставку и список навыков текущего пользователя. " +
        "Профиль должен существовать (создаётся отдельным эндпоинтом POST /api/freelancer-profiles). " +
        "Rating через этот эндпоинт не изменяется — он обновляется только на основании отзывов.")]
    public static async Task<IResult> Handle(
        [FromBody] UpdateProfileRequest request,
        HttpContext ctx,
        IValidator<UpdateProfileRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<FreelancerProfileDto>.BadRequest(ProfileTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var profile = await db.FreelancerProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(fp => fp.UserId == currentUserId, ct);

        if (profile == null)
            return Result<FreelancerProfileDto>.NotFound(ProfileTextResponses.NotFound).ToHttpResult();

        var skills = await db.Skills.Where(s => request.SkillIds.Select(Guid.Parse).Contains(s.Id)).ToListAsync(ct);
        if (skills.Count != request.SkillIds.Distinct().Count())
            return Result<FreelancerProfileDto>.BadRequest(ProfileTextResponses.SkillsNotFound).ToHttpResult();

        profile.Bio = request.Bio;
        profile.HourlyRate = request.HourlyRate;
        profile.Skills = skills;

        await db.SaveChangesAsync(ct);

        return Result<FreelancerProfileDto>.Ok(FreelancerProfileDto.FromEntity(profile), ProfileTextResponses.Updated).ToHttpResult();
    }
}
