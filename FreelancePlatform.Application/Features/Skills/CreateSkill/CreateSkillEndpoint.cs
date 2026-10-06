using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Skills.CreateSkill;

public static class CreateSkillEndpoint
{
    [WolverinePost("/api/v1/skills"), Tags("Skills")]
    [EndpointSummary("Создание нового умения")]
    [EndpointDescription("Возвращает сообщение об успешном создании умения и объект умения.")]
    public static async Task<IResult> Handle(
        CreateSkillRequest request,
        IValidator<CreateSkillRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<SkillDto>.BadRequest(SkillsTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Skills.AnyAsync(s => s.NameNormalized == request.Name.ToLowerInvariant(), ct);
        if (exists)
            return Result<SkillDto>.Conflict(SkillsTextResponses.AlreadyExists).ToHttpResult();

        var skill = new SkillEntity { Name = request.Name.Trim(), NameNormalized = request.Name.ToLowerInvariant() };
        db.Skills.Add(skill);
        await db.SaveChangesAsync(ct);

        return Result<SkillDto>.Created(new SkillDto(skill.Id, skill.Name), SkillsTextResponses.Created).ToHttpResult();
    }
}
