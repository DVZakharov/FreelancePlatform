using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Reviews.GetFreelancerReviews;

public static class GetFreelancerReviewsEndpoint
{
    [WolverineGet("/api/v1/freelancer-profiles/{id}/reviews"), Tags("Reviews")]
    [EndpointSummary("Отзывы о фрилансере")]
    [EndpointDescription("Возвращает все отзывы, оставленные фрилансеру заказчиками по завершённым контрактам. Публичный эндпоинт.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetFreelancerReviewsRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetFreelancerReviewsRequest(id);
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<List<ReviewDto>>.BadRequest(
                ReviewTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var profile = await db.FreelancerProfiles.FirstOrDefaultAsync(fp => fp.Id == Guid.Parse(id), ct);
        if (profile is null)
            return Result<List<ReviewDto>>.NotFound(ReviewTextResponses.ProfileNotFound).ToHttpResult();

        var reviews = await db.Reviews
            .Where(r => r.TargetId == profile.UserId)
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