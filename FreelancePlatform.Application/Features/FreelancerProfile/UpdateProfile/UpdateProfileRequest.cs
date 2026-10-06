namespace FreelancePlatform.Application.Features.FreelancerProfile.UpdateProfile;

public record UpdateProfileRequest(string Bio, decimal HourlyRate, List<string> SkillIds);