namespace FreelancePlatform.Application.Features.Admin.DemoteFromAdmin;

public record DemoteFromAdminRequest(string Email, string SetupSecret);