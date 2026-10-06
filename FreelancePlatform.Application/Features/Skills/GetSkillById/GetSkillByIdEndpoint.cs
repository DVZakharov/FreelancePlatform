using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Skills.GetSkillById;

public static class GetSkillByIdEndpoint
{
    [WolverineGet("/api/v1/skills/{id}"), Tags("Skills")]
    [EndpointSummary("Получить навык по ID")]
    [EndpointDescription("Возвращает навык по её уникальному идентификатору.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetSkillByIdRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetSkillByIdRequest(id);

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<SkillDto>.BadRequest(SkillsTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var entity = db.Skills
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == Guid.Parse(request.Id));

        if (entity == null)
            return Result<SkillDto>.NotFound(SkillsTextResponses.NotFound).ToHttpResult();

        return Result<SkillDto>.Ok(new SkillDto(entity.Id, entity.Name), SkillsTextResponses.GetSuccess).ToHttpResult();
    }
}
