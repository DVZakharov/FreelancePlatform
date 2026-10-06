using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Skills.UpdateSkill;

public static class UpdateSkillEndpoint
{
    [WolverinePut("/api/v1/skills/{id}"), Tags("Skills")]
    [EndpointSummary("Обновление существующего навыка")]
    [EndpointDescription("Возвращает сообщение об успешном обновлении навыка с объектом навыка.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] UpdateSkillRequest request,
        IValidator<UpdateSkillRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var skillId))
            return Result<DisputeDto>.BadRequest(SkillsTextResponses.ValidationFailed, [SkillsTextResponses.InvalidId]).ToHttpResult();
        
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<SkillDto>.BadRequest(SkillsTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Skills.FirstOrDefaultAsync(s => s.Id == skillId, ct);
        if (exists == null)
            return Result<SkillDto>.NotFound(SkillsTextResponses.NotFound).ToHttpResult();

        var nameExists = await db.Skills.AnyAsync(s => s.NameNormalized == request.Name.ToLowerInvariant() && s.Id != exists.Id, ct);
        if (nameExists)
            return Result<SkillDto>.Conflict(SkillsTextResponses.AlreadyExists).ToHttpResult();

        exists.Name = request.Name.Trim();
        exists.NameNormalized = request.Name.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(ct);
        return Result<SkillDto>.Ok(new SkillDto(exists.Id, exists.Name), SkillsTextResponses.Updated).ToHttpResult();
    }
}