using FreelancePlatform.Application.Common.Abstracts;

namespace FreelancePlatform.Application.Features.FreelancerProfiles.SearchProfiles;

public class SearchProfilesRequest : ListRequest
{
    public List<string>? SkillIds { get; set; } = null;
    public decimal? MinRating { get; set; } = null;
}