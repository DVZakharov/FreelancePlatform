namespace FreelancePlatform.Application.Features.Projects;

public static class ProjectPublishRules
{
    public static List<string> Validate(
        string? title, string? description, decimal budgetMin, decimal budgetMax,
        DateTime deadline, int skillsCount)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title))
            errors.Add(ProjectTextResponses.TitleRequired);
        if (string.IsNullOrWhiteSpace(description) || description.Length < 50)
            errors.Add(ProjectTextResponses.InvalidDescriptionMinLenght);
        if (budgetMin <= 0 || budgetMax <= 0)
            errors.Add(ProjectTextResponses.InvalidBudget);
        if (deadline <= DateTime.UtcNow)
            errors.Add(ProjectTextResponses.InvalidDeadline);
        if (skillsCount == 0)
            errors.Add(ProjectTextResponses.OneOrMoreRequiredSkills);
        return errors;
    }
}