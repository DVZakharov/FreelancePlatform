using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Payments.TopUpBalance;

public static class TopUpBalanceEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/payments/top-up"), Tags("Payments")]
    [EndpointSummary("Пополнение баланса")]
    [EndpointDescription("Имитация пополнения баланса без реальной интеграции с платёжным шлюзом — " +
                         "сумма сразу зачисляется на счёт пользователя. Используется для тестирования эскроу-механизма платформы.")]
    public static async Task<IResult> Handle(
        [FromBody] TopUpBalanceRequest request,
        HttpContext ctx,
        IValidator<TopUpBalanceRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<TransactionDto>.BadRequest(
                PaymentTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var user = await db.Users.FindAsync([currentUserId], ct);
        user!.Balance += request.Amount;

        var transaction = new TransactionEntity
        {
            UserId = currentUserId,
            Type = TransactionType.TopUp,
            Amount = request.Amount,
            BalanceAfter = user.Balance,
            Description = "Balance top-up"
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        return Result<TransactionDto>.Created(TransactionDto.FromEntity(transaction), PaymentTextResponses.TopUpSuccess).ToHttpResult();
    }
}