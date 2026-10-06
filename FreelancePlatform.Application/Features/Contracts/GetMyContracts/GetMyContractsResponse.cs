using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Contracts.GetMyContracts;

public record GetMyContractsResponse(List<MyContractDto> Items);