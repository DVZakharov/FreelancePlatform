namespace FreelancePlatform.Application.Features.Milestones.CreateMilestone;

public record CreateMilestoneRequest(string Title, decimal Amount, DateTime? DueDate);