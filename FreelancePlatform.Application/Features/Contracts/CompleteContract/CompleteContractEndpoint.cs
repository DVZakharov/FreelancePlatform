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

namespace FreelancePlatform.Application.Features.Contracts.CompleteContract;

public static class CompleteContractEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/contracts/{id}/complete"), Tags("Contracts")]
    [EndpointSummary("Завершить контракт")]
    [EndpointDescription("Переводит контракт в статус Completed. Доступно заказчику, только когда все " +
        "этапы контракта находятся в статусе Completed. Проект также переходит в статус Completed.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<CompleteContractRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new CompleteContractRequest(id);
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Result<ContractDto>.BadRequest(ContractTextResponses.ValidationError, [.. validationResult.Errors.Select(x => x.ErrorMessage)]).ToHttpResult();
        
        var currentUserId = ctx.GetCurrentUserId();

        var contract = await db.Contracts
            .Include(c => c.Milestones)
            .Include(c => c.Project)
            .Include(c => c.Client)
            .Include(c => c.Freelancer)
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(id), ct);

        if (contract is null)
            return Result<ContractDto>.NotFound(ContractTextResponses.NotFound).ToHttpResult();

        if (contract.ClientId != currentUserId)
            return Result<ContractDto>.Forbidden(ContractTextResponses.NotClient).ToHttpResult();

        if (contract.Status != ContractStatus.Active)
            return Result<ContractDto>.Conflict(ContractTextResponses.NotActive).ToHttpResult();

        if (contract.Milestones.Count == 0 || contract.Milestones.Any(m => m.Status != MilestoneStatus.Completed))
            return Result<ContractDto>.Conflict(ContractTextResponses.MilestonesNotCompleted).ToHttpResult();

        contract.Status = ContractStatus.Completed;
        contract.CompletedAt = DateTime.UtcNow;
        contract.Project.Status = ProjectStatus.Completed;

        await db.SaveChangesAsync(ct);

        return Result<ContractDto>.Ok(ContractDto.FromEntity(contract), ContractTextResponses.Completed).ToHttpResult();
    }
}