namespace FreelancePlatform.Application.Features.Projects.CreateProject;

public record CreateProjectRequest(
    string CategoryId,
    string Title,
    string Description,
    string BudgetType,
    decimal BudgetMin,
    decimal BudgetMax,
    DateTime Deadline,
    List<string> SkillIds);