namespace FreelancePlatform.Application.Features.FreelancerProfiles.UpdateProfile;

public record UpdateProfileRequest(string Bio, decimal HourlyRate, List<string> SkillIds);