using FluentValidation;
using FreelancePlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Messages.SendMessage;

public static class SendMessageEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/contracts/{contractId}/messages"), Tags("Messages")]
    [EndpointSummary("Отправить сообщение")]
    [EndpointDescription("Отправляет сообщение в рамках контракта. Доступно только участникам контракта — заказчику и фрилансеру.")]
    public static async Task<IResult> Handle(
        [FromRoute] string contractId,
        [FromBody] SendMessageRequest request,
        HttpContext ctx,
        IValidator<SendMessageRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<MessageDto>.BadRequest(
                MessageTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(contractId, out var contractGuid))
            return Result<MessageDto>.BadRequest(MessageTextResponses.ValidationError, [MessageTextResponses.InvalidContractId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractGuid, ct);
        if (contract is null)
            return Result<MessageDto>.NotFound(MessageTextResponses.ContractNotFound).ToHttpResult();

        if (contract.ClientId != currentUserId && contract.FreelancerId != currentUserId)
            return Result<MessageDto>.Forbidden(MessageTextResponses.NotParticipant).ToHttpResult();

        var message = new MessageEntity
        {
            ContractId = contractGuid,
            SenderId = currentUserId,
            Content = request.Content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        var reloaded = await db.Messages
            .Include(m => m.Sender)
            .FirstAsync(m => m.Id == message.Id, ct);

        return Result<MessageDto>.Created(MessageDto.FromEntity(reloaded), MessageTextResponses.Sent).ToHttpResult();
    }
}