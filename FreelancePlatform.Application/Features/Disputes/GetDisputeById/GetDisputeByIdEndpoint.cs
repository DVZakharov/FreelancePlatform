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

namespace FreelancePlatform.Application.Features.Disputes.GetDisputeById;

public static class GetDisputeByIdEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/disputes/{id}"), Tags("Disputes")]
    [EndpointSummary("Получить спор по ID")]
    [EndpointDescription("Возвращает детали спора. Доступно участникам контракта (заказчику, фрилансеру) и администраторам.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetDisputeByIdRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetDisputeByIdRequest(id);
        
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<DisputeDto>.BadRequest(DisputeTextResponses.ValidationError, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var dispute = await db.Disputes
            .Include(d => d.Contract)
            .FirstOrDefaultAsync(d => d.Id == Guid.Parse(id), ct);

        if (dispute is null)
            return Result<DisputeDto>.NotFound(DisputeTextResponses.NotFound).ToHttpResult();

        var user = await db.Users.FindAsync([currentUserId], ct);
        var isParticipant = dispute.Contract.ClientId == currentUserId || dispute.Contract.FreelancerId == currentUserId;
        var isAdmin = user!.Role.HasFlag(UserRole.Admin);

        if (!isParticipant && !isAdmin)
            return Result<DisputeDto>.Forbidden(DisputeTextResponses.NotParticipant).ToHttpResult();

        return Result<DisputeDto>.Ok(DisputeDto.FromEntity(dispute), DisputeTextResponses.GetSuccess).ToHttpResult();
    }
}