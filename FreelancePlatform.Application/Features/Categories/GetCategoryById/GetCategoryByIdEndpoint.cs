using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Categories.GetCategoryById;

public static class GetCategoryByIdEndpoint
{
    [WolverineGet("/api/v1/categories/{id}"), Tags("Categories")]
    [EndpointSummary("Получить категорию по ID")]
    [EndpointDescription("Возвращает категорию по её уникальному идентификатору.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetCategoryByIdRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetCategoryByIdRequest(id);

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<CategoryDto>.BadRequest(CategoriesTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();

        var entity = db.Categories
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == Guid.Parse(request.Id));

        if (entity == null)
            return Result<CategoryDto>.NotFound(CategoriesTextResponses.NotFound).ToHttpResult();

        return Result<CategoryDto>.Ok(new CategoryDto(entity.Id, entity.Name), CategoriesTextResponses.GetSuccess).ToHttpResult();
    }
}