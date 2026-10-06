using FreelancePlatform.Application.DTOs;

namespace FreelancePlatform.Application.Features.Payments.GetTransactionHistory;

public record GetTransactionHistoryResponse(List<TransactionDto> Items, int TotalCount, int Page, int PageSize);