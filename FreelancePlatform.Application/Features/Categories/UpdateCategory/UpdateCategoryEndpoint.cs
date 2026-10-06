using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Categories.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    [WolverinePut("/api/v1/categories/{id}"), Tags("Categories")]
    [EndpointSummary("Обновление существующей категории")]
    [EndpointDescription("Возвращает сообщение об успешном обновлении категории с объектом категории.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] UpdateCategoryRequest request,
        IValidator<UpdateCategoryRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var categoryId))
            return Result<DisputeDto>.BadRequest(CategoriesTextResponses.ValidationFailed, [CategoriesTextResponses.InvalidId]).ToHttpResult();
        
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<CategoryDto>.BadRequest(CategoriesTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (exists == null)
            return Result<CategoryDto>.NotFound(CategoriesTextResponses.NotFound).ToHttpResult();

        var nameExists = await db.Categories.AnyAsync(c => c.NameNormalized == request.Name.ToLowerInvariant() && c.Id != exists.Id, ct);
        if (nameExists)
            return Result<CategoryDto>.Conflict(CategoriesTextResponses.AlreadyExists).ToHttpResult();

        exists.Name = request.Name.Trim();
        exists.NameNormalized = request.Name.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(ct);
        return Result<CategoryDto>.Ok(new CategoryDto(exists.Id, exists.Name), CategoriesTextResponses.Updated).ToHttpResult();
    }
}