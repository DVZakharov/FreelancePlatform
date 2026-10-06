using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.FreelancerProfile.SearchProfiles;

public static class SearchProfileEndpoint
{
    [WolverineGet("/api/v1/freelancer-profiles"), Tags("FreelancerProfiles")]
    [EndpointSummary("Поиск профилей фрилансеров")]
    [EndpointDescription("Возвращает список профилей фрилансеров с постраничной разбивкой. " +
        "Поддерживает фильтрацию по нескольким навыкам одновременно — фрилансер должен обладать " +
        "ВСЕМИ указанными навыками (логика AND) — и минимальному рейтингу. " +
        "Публичный эндпоинт, доступен без авторизации.")]
    public static async Task<IResult> Handle(
        [FromQuery] SearchProfilesRequest request,
        IValidator<SearchProfilesRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ListFreelancerProfilesDto>.BadRequest(
                ProfileTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var query = db.FreelancerProfiles
            .Include(fp => fp.User)
            .Include(fp => fp.Skills)
            .AsQueryable();

        if (request.SkillIds is { Count: > 0 })
        {
            var skillGuids = request.SkillIds.Select(Guid.Parse).ToList();

            // AND: у профиля должны быть ВСЕ указанные навыки
            foreach (var skillId in skillGuids)
            {
                var id = skillId; // локальная копия для замыкания в цикле
                query = query.Where(fp => fp.Skills.Any(s => s.Id == id));
            }
        }

        if (request.MinRating.HasValue)
            query = query.Where(fp => fp.Rating >= request.MinRating.Value);

        var totalCount = await query.CountAsync(ct);

        var profiles = await query
            .OrderByDescending(fp => fp.Rating)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = profiles.Select(FreelancerProfileDto.FromEntity).ToList();
        var response = new ListFreelancerProfilesDto
        {
            RequestedPageNumber = request.PageNumber,
            RequestedPageSize = request.PageSize,
            TotalCount = totalCount,
            ReturnedCount = dtos.Count
        };

        return Result<ListFreelancerProfilesDto>.Ok(response, ProfileTextResponses.SearchSuccess).ToHttpResult();
    }
}
