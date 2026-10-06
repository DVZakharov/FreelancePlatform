using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Categories.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    [WolverineDelete("/api/v1/categories/{id}"), Tags("Categories")]
    [EndpointSummary("Удаление категории")]
    [EndpointDescription("Возвращает сообщение об успешном удалении категории с указанным идентификатором.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<DeleteCategoryRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new DeleteCategoryRequest(id);

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<object>.BadRequest(CategoriesTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Categories.FirstOrDefaultAsync(c => c.Id == Guid.Parse(request.Id), ct);
        if (exists == null)
            return Result<object>.NotFound(CategoriesTextResponses.NotFound).ToHttpResult();

        db.Categories.Remove(exists);
        await db.SaveChangesAsync(ct);
        return Result<object>.Ok(new { Id = exists.Id }, CategoriesTextResponses.DeletedWithName(exists.Name)).ToHttpResult();
    }
}