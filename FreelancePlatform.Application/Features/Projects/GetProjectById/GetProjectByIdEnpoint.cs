using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Projects.GetProjectById;

public static class GetProjectByIdEndpoint
{
    [WolverineGet("/api/v1/projects/{id}"), Tags("Projects")]
    [EndpointSummary("Получить проект по ID")]
    [EndpointDescription("Возвращает проект по идентификатору. Проекты в статусе Draft видны только " +
        "их владельцу; остальные статусы доступны публично, включая неавторизованных пользователей.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<GetProjectByIdRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new GetProjectByIdRequest(id);
        
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProjectDto>.BadRequest(
                ProjectTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var project = await db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id), ct);

        if (project is null)
            return Result<ProjectDto>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();

        if (project.Status == ProjectStatus.Draft)
        {
            if (!ctx.User.Identity?.IsAuthenticated ?? true)
                return Result<ProjectDto>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();

            var currentUserId = ctx.GetCurrentUserId();
            if (project.ClientId != currentUserId)
                return Result<ProjectDto>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();
        }

        return Result<ProjectDto>.Ok(ProjectDto.FromEntity(project), ProjectTextResponses.GetSuccess).ToHttpResult();
    }
}