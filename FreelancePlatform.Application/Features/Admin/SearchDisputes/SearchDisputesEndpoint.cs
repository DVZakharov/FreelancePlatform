using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Admin.SearchDisputes;

public static class SearchDisputesEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverineGet("/api/v1/admin/disputes"), Tags("Admin")]
    [EndpointSummary("Очередь споров")]
    [EndpointDescription("Возвращает постраничный список споров всех контрактов с фильтром по статусу " +
        "(например, Open — нерассмотренные). Доступно только администраторам.")]
    public static async Task<IResult> Handle(
        [AsParameters] SearchDisputesRequest request,
        IValidator<SearchDisputesRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SearchDisputesResponse>.BadRequest(
                AdminTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var query = db.Disputes.AsQueryable();

        if (request.Status is not null)
        {
            var status = Enum.Parse<DisputeStatus>(request.Status, true);
            query = query.Where(d => d.Status == status);
        }

        var totalCount = await query.CountAsync(ct);

        var disputes = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = disputes.Select(DisputeDto.FromEntity).ToList();
        var response = new SearchDisputesResponse(request.PageNumber, request.PageSize, totalCount, dtos.Count ,dtos);

        return Result<SearchDisputesResponse>.Ok(response, AdminTextResponses.DisputesRetrieved).ToHttpResult();
    }
}