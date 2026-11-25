using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Payments.GetTransactionHistory;

public static class GetTransactionHistoryEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/payments/history"), Tags("Payments")]
    [EndpointSummary("История транзакций")]
    [EndpointDescription("Возвращает историю всех денежных операций текущего пользователя (пополнения, " +
        "заморозки, переводы, возвраты) с постраничной разбивкой и опциональной фильтрацией по типу.")]
    public static async Task<IResult> Handle(
        [FromQuery] GetTransactionHistoryRequest request,
        IValidator<GetTransactionHistoryRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<GetTransactionHistoryResponse>.BadRequest(
                PaymentTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var query = db.Transactions.Where(t => t.UserId == currentUserId).AsQueryable();

        if (request.Type is not null)
        {
            var type = Enum.Parse<TransactionType>(request.Type, true);
            query = query.Where(t => t.Type == type);
        }

        var totalCount = await query.CountAsync(ct);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = transactions.Select(TransactionDto.FromEntity).ToList();
        var response = new GetTransactionHistoryResponse(dtos, totalCount, request.PageNumber, request.PageSize);

        return Result<GetTransactionHistoryResponse>.Ok(response, PaymentTextResponses.HistoryRetrieved).ToHttpResult();
    }
}