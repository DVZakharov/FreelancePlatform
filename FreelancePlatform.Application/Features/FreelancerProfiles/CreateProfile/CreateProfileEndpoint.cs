using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Users;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.CreateProfile;

public static class CreateProfileEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/freelancer-profiles"), Tags("FreelancerProfiles")]
    [EndpointSummary("Создание профиля фрилансера")]
    [EndpointDescription("Создаёт профиль фрилансера для текущего пользователя. " +
                "Пользователь автоматически получает роль Freelancer. " +
                "Профиль можно создать только один раз на аккаунт.")]
    public static async Task<IResult> Handle(
       [FromBody] CreateProfileRequest request,
       HttpContext ctx,
       IValidator<CreateProfileRequest> validator,
       IApplicationDbContext db,
       CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<FreelancerProfileDto>.BadRequest(ProfileTextResponses.ValidationError, [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var user = await db.Users.FindAsync([currentUserId], ct);
        if (user is null)
            return Result<FreelancerProfileDto>.NotFound(UserTextResponses.NotFound).ToHttpResult();

        var exists = await db.FreelancerProfiles.AnyAsync(fp => fp.UserId == currentUserId, ct);
        if (exists)
            return Result<FreelancerProfileDto>.Conflict(ProfileTextResponses.AlreadyExists).ToHttpResult();

        var skills = await db.Skills.Where(s => request.SkillIds.Select(x => Guid.Parse(x)).Contains(s.Id)).ToListAsync(ct);
        if (skills.Count != request.SkillIds.Distinct().Count())
            return Result<FreelancerProfileDto>.BadRequest(ProfileTextResponses.SkillsNotFound).ToHttpResult();

        var profile = new FreelancerProfileEntity
        {
            UserId = currentUserId,
            Bio = request.Bio,
            HourlyRate = request.HourlyRate,
            Skills = skills,
            User = user
        };

        db.FreelancerProfiles.Add(profile);
        user.Role |= UserRole.Freelancer;

        await db.SaveChangesAsync(ct);

        return Result<FreelancerProfileDto>.Created(FreelancerProfileDto.FromEntity(profile), ProfileTextResponses.Created).ToHttpResult();
    }
}