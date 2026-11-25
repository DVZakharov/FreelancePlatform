using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Users.UpdateMe;

public static class UpdateMeEndpoint
{
    [Authorize]
    [WolverinePut("/api/v1/users/me"), Tags("Users")]
    [EndpointSummary("Обновить данные текущего пользователя")]
    [EndpointDescription("Обновляет имя текущего пользователя. Email через этот эндпоинт не меняется.")]
    public static async Task<IResult> Handle(
        [FromBody] UpdateMeRequest request,
        HttpContext ctx,
        IValidator<UpdateMeRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<UserDto>.BadRequest(
                UserTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var user = await db.Users.FindAsync([currentUserId], ct);
        if (user is null)
            return Result<UserDto>.NotFound(UserTextResponses.NotFound).ToHttpResult();

        user.FullName = request.FullName;
        await db.SaveChangesAsync(ct);

        var isFreelancer = await db.FreelancerProfiles.AnyAsync(fp => fp.UserId == currentUserId, ct);

        return Result<UserDto>.Ok(UserDto.FromEntity(user, isFreelancer), UserTextResponses.Updated).ToHttpResult();
    }
}