namespace FreelancePlatform.Application.Features.Projects.UpdateProject;

public record UpdateProjectRequest(
    string CategoryId,
    string Title,
    string Description,
    string BudgetType,
    decimal BudgetMin,
    decimal BudgetMax,
    DateTime Deadline,
    List<string> SkillIds);