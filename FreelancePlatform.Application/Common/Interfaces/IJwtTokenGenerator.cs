using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(UserEntity user);
}
