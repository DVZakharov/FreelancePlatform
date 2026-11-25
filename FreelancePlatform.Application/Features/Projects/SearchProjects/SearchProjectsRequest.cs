using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Features.Projects.SearchProjects;

public class SearchProjectsRequest : ListRequest
{
    public string? CategoryId { get; set; }
    public List<string>? SkillIds { get; set; } = null!;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
}