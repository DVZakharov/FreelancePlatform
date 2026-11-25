using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Messages.GetContractMessages;

public static class GetContractMessagesEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/contracts/{contractId}/messages"), Tags("Messages")]
    [EndpointSummary("Сообщения по контракту")]
    [EndpointDescription("Возвращает переписку в рамках контракта с постраничной разбивкой, " +
        "от новых к старым. Доступно только участникам контракта.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        [FromQuery] GetContractMessagesRequest request,
        IValidator<GetContractMessagesRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<GetContractMessagesResponse>.BadRequest(
                MessageTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(contractId, out var contractGuid))
            return Result<GetContractMessagesResponse>.BadRequest(MessageTextResponses.ValidationError, [MessageTextResponses.InvalidContractId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractGuid, ct);
        if (contract is null)
            return Result<GetContractMessagesResponse>.NotFound(MessageTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<GetContractMessagesResponse>.Forbidden(MessageTextResponses.NotParticipant).ToHttpResult();

        var query = db.Messages.Include(m => m.Sender).Where(m => m.ContractId == contractGuid);

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = messages.Select(MessageDto.FromEntity).ToList();
        var response = new GetContractMessagesResponse(dtos, totalCount, request.PageNumber, request.PageSize);

        return Result<GetContractMessagesResponse>.Ok(response, MessageTextResponses.GetSuccess).ToHttpResult();
    }
}