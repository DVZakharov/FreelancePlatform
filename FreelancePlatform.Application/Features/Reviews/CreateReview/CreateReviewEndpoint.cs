// CreateReviewEndpoint.cs
using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Reviews.CreateReview;

public static class CreateReviewEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/contracts/{contractId}/reviews"), Tags("Reviews")]
    [EndpointSummary("Оставить отзыв")]
    [EndpointDescription("Оставляет отзыв другой стороне контракта. Доступно только после завершения " +
        "контракта (статус Completed), каждая сторона может оставить только один отзыв на контракт. " +
        "Если получатель отзыва — фрилансер, его средний рейтинг автоматически пересчитывается.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        [FromBody] CreateReviewRequest request,
        HttpContext ctx,
        IValidator<CreateReviewRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ReviewDto>.BadRequest(
                ReviewTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(contractId, out var contractGuid))
            return Result<ReviewDto>.BadRequest(ReviewTextResponses.ValidationError, [ReviewTextResponses.InvalidContractId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractGuid, ct);
        if (contract is null)
            return Result<ReviewDto>.NotFound(ReviewTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<ReviewDto>.Forbidden(ReviewTextResponses.NotParticipant).ToHttpResult();

        if (contract.Status != ContractStatus.Completed)
            return Result<ReviewDto>.Conflict(ReviewTextResponses.ContractNotCompleted).ToHttpResult();

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ContractId == contractGuid && r.AuthorId == currentUserId, ct);
        if (alreadyReviewed)
            return Result<ReviewDto>.Conflict(ReviewTextResponses.AlreadyReviewed).ToHttpResult();

        var targetId = contract.ClientId == currentUserId ? contract.FreelancerId : contract.ClientId;

        var review = new ReviewEntity
        {
            ContractId = contractGuid,
            AuthorId = currentUserId,
            TargetId = targetId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);

        // Если получатель — фрилансер, пересчитываем его средний рейтинг
        var targetProfile = await db.FreelancerProfiles.FirstOrDefaultAsync(fp => fp.UserId == targetId, ct);
        if (targetProfile is not null)
        {
            var existingRatings = await db.Reviews
                .Where(r => r.TargetId == targetId)
                .Select(r => r.Rating)
                .ToListAsync(ct);

            existingRatings.Add(request.Rating); // включаем новый отзыв, который ещё не сохранён
            targetProfile.Rating = (decimal)existingRatings.Average();
        }

        await db.SaveChangesAsync(ct);

        var authorName = (await db.Users.FindAsync([currentUserId], ct))!.FullName;
        var targetName = (await db.Users.FindAsync([targetId], ct))!.FullName;

        return Result<ReviewDto>.Created(
            ReviewDto.FromEntity(review, authorName, targetName),
            ReviewTextResponses.Created).ToHttpResult();
    }
}