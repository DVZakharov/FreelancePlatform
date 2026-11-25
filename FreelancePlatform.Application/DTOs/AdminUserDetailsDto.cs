namespace FreelancePlatform.Application.DTOs;

public record AdminUserDetailsDto(
    UserDto User,
    int ProjectsAsClientCount,
    int ProposalsCount,
    int ContractsCount,
    int ActiveSessionsCount,
    int TransactionsCount);