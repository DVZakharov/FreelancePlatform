namespace FreelancePlatform.Application.DTOs;

public record PlatformStatsDto(
    int TotalUsers,
    int TotalAdmins,
    int TotalFreelancers,
    Dictionary<string, int> ProjectsByStatus,
    Dictionary<string, int> ContractsByStatus,
    int OpenDisputes,
    decimal TotalEscrowHeld,
    decimal TotalUsersBalance,
    decimal TotalToppedUp);