using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Categories.ListCategories;

public static class ListCategoriesEndpoint
{
    [WolverineGet("/api/v1/categories"), Tags("Categories")]
    [EndpointSummary("Получить список категорий постранично")]
    [EndpointDescription("""
        Возвращает постраничный список категорий, отсортированных по имени.
        Если параметры страницы не заданы, используются значения по умолчанию.
        """)]
    public static async Task<IResult> Handle(
        [FromQuery] ListCategoriesRequest request,
        IValidator<ListCategoriesRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<ListCategoriesResponse>.BadRequest(CategoriesTextResponses.ValidationFailed, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();


        var query = db.Categories.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var listCategoriesDto = new ListCategoriesResponse
        {
            RequestedPageNumber = request.PageNumber,
            RequestedPageSize = request.PageSize,
            TotalCount = totalCount,
            ReturnedCount = categories.Count,
            Categories = [.. categories.Select(c => new CategoryDto(c.Id, c.Name))]
        };

        return Result<ListCategoriesResponse>.Ok(listCategoriesDto, CategoriesTextResponses.GetsSuccess).ToHttpResult();
    }
}