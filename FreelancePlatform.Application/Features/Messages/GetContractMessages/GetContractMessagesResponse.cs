using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Messages.GetContractMessages;

public record GetContractMessagesResponse(List<MessageDto> Items, int TotalCount, int Page, int PageSize);