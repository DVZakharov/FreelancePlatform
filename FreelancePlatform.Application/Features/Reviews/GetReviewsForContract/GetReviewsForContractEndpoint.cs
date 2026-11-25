using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Reviews.GetReviewsForContract;

public static class GetReviewsForContractEndpoint
{
    [WolverineGet("/api/v1/contracts/{contractId}/reviews"), Tags("Reviews")]
    [EndpointSummary("Отзывы по контракту")]
    [EndpointDescription("Возвращает все отзывы (от заказчика и от фрилансера), оставленные в рамках указанного контракта. Публичный эндпоинт.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        IValidator<GetReviewsForContractRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetReviewsForContractRequest(contractId);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<List<ReviewDto>>.BadRequest(
                ReviewTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var reviews = await db.Reviews
            .Where(r => r.ContractId == Guid.Parse(contractId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (reviews.Count == 0)
            return Result<List<ReviewDto>>.Ok([], ReviewTextResponses.GetSuccess).ToHttpResult();

        var userIds = reviews.SelectMany(r => new[] { r.AuthorId, r.TargetId }).Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var dtos = reviews
            .Select(r => ReviewDto.FromEntity(r, users[r.AuthorId], users[r.TargetId]))
            .ToList();

        return Result<List<ReviewDto>>.Ok(dtos, ReviewTextResponses.GetSuccess).ToHttpResult();
    }
}