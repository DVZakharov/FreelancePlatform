using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Skills.ListSkills;

public static class ListSkillsEndpoint
{
    [WolverineGet("/api/v1/skills"), Tags("Skills")]
    [EndpointSummary("Получить список навыков постранично")]
    [EndpointDescription("""
        Возвращает постраничный список навыков, отсортированных по имени.
        Если параметры страницы не заданы, используются значения по умолчанию.
        """)]
    public static async Task<IResult> Handle(
        [FromQuery] ListSkillsRequest request,
        IValidator<ListSkillsRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<ListSkillsDto>.BadRequest(SkillsTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();


        var query = db.Skills.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var skills = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var listSkillsDto = new ListSkillsDto
        {
            RequestedPageNumber = request.PageNumber,
            RequestedPageSize = request.PageSize,
            TotalCount = totalCount,
            ReturnedCount = skills.Count,
            Skills = [.. skills.Select(s => new SkillDto(s.Id, s.Name))]
        };

        return Result<ListSkillsDto>.Ok(listSkillsDto, SkillsTextResponses.GetsSuccess).ToHttpResult();
    }
}