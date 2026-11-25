using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.GetProfileById;

public static class GetProfileByIdEndpoint
{
    [WolverineGet("/api/v1/freelancer-profiles/{id}"), Tags("FreelancerProfiles")]
    [EndpointSummary("Получить профиль фрилансера по ID")]
    [EndpointDescription("Публичный эндпоинт — возвращает профиль фрилансера по его идентификатору. " +
    "Доступен без авторизации, используется заказчиками для просмотра исполнителей.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetProfileByIdRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetProfileByIdRequest(id);

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<FreelancerProfileDto>.BadRequest(
                ProfileTextResponses.ValidationError, 
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var profile = await db.FreelancerProfiles
            .Include(fp => fp.User)
            .Include(fp => fp.Skills)
            .FirstOrDefaultAsync(fp => fp.Id == Guid.Parse(request.Id), ct);

        if (profile == null)
            return Result<FreelancerProfileDto>.NotFound(ProfileTextResponses.NotFound).ToHttpResult();

        return Result<FreelancerProfileDto>.Ok(FreelancerProfileDto.FromEntity(profile), ProfileTextResponses.GetSuccess).ToHttpResult();
    }
}
