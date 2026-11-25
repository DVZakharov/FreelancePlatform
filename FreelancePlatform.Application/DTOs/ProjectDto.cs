using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record ProjectDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string Description,
    string BudgetType,
    decimal BudgetMin,
    decimal BudgetMax,
    DateTime Deadline,
    string Status,
    List<SkillDto> RequiredSkills,
    DateTime? PublishedAt,
    DateTime CreatedAt)
{
    public static ProjectDto FromEntity(ProjectEntity entity) =>
        new(
            entity.Id,
            entity.ClientId,
            entity.Client.FullName,
            entity.CategoryId,
            entity.Category.Name,
            entity.Title,
            entity.Description,
            entity.BudgetType.ToString(),
            entity.BudgetMin,
            entity.BudgetMax,
            entity.Deadline,
            entity.Status.ToString(),
            entity.RequiredSkills.Select(s => new SkillDto(s.Id, s.Name)).ToList(),
            entity.PublishedAt,
            entity.CreatedAt);
}