namespace FreelancePlatform.Application.Features.FreelancerProfiles.CreateProfile;

public record CreateProfileRequest(string Bio, decimal HourlyRate, List<string> SkillIds);