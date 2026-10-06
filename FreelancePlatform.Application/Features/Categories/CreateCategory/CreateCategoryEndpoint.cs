using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Categories.CreateCategory;

public static class CreateCategoryEndpoint
{
    [WolverinePost("/api/v1/categories"), Tags("Categories")]
    [EndpointSummary("Создание новой категории")]
    [EndpointDescription("Возвращает сообщение об успешном создании категории и объект категории.")]
    public static async Task<IResult> Handle(
        [FromBody] CreateCategoryRequest request,
        IValidator<CreateCategoryRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<CategoryDto>.BadRequest(CategoriesTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var exists = await db.Categories.AnyAsync(c => c.NameNormalized == request.Name.ToLowerInvariant(), ct);
        if (exists)
            return Result<CategoryDto>.Conflict(CategoriesTextResponses.AlreadyExists).ToHttpResult();

        var category = new CategoryEntity { Name = request.Name.Trim(), NameNormalized = request.Name.ToLowerInvariant() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return Result<CategoryDto>.Created(new CategoryDto(category.Id, category.Name), CategoriesTextResponses.CreatedWithName(category.Name)).ToHttpResult();
    }
}