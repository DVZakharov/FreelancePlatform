using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Categories.ListCategories;

public sealed class ListCategoriesResponse
{
    public int RequestedPageNumber { get; init; }
    public int RequestedPageSize { get; init; }
    
    public int TotalCount { get; init; }
    public int ReturnedCount { get; init; }

    public List<CategoryDto> Categories { get; init; } = [];
}