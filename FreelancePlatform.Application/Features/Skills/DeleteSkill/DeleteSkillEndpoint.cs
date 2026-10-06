using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Skills.DeleteSkill;

public static class DeleteSkillEndpoint
{
    [WolverineDelete("/api/v1/skills/{id}"), Tags("Skills")]
    [EndpointSummary("Удаление умения")]
    [EndpointDescription("Возвращает сообщение об успешном удалении умения с указанным идентификатором.")]
    public static async Task<IResult> Handle(
        string id,
        IValidator<DeleteSkillRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new DeleteSkillRequest(id);

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<object>.BadRequest(SkillsTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Skills.FirstOrDefaultAsync(s => s.Id == Guid.Parse(request.Id), ct);
        if (exists == null)
            return Result<object>.NotFound(SkillsTextResponses.NotFound).ToHttpResult();

        db.Skills.Remove(exists);
        await db.SaveChangesAsync(ct);
        return Result<object>.Ok(new { Id = exists.Id }, SkillsTextResponses.Deleted).ToHttpResult();
    }
}