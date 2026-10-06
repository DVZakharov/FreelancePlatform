using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Projects.PublishProject;

public static class PublishProjectEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/projects/{id}/publish"), Tags("Projects")]
    [EndpointSummary("Публикация проекта")]
    [EndpointDescription("Переводит проект из статуса Draft в Open, делая его видимым в публичном поиске " +
        "и открывая приём откликов. Требует строгой валидации: непустое название/описание, " +
        "корректный диапазон бюджета, дедлайн не в прошлом, хотя бы один требуемый навык.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        IValidator<PublishProjectRequest> validator,
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var request = new PublishProjectRequest(id);
        
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProjectDto>.BadRequest(
                ProjectTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var project = await db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(id), ct);

        if (project is null)
            return Result<ProjectDto>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();

        if (project.ClientId != currentUserId)
            return Result<ProjectDto>.Forbidden(ProjectTextResponses.NotOwner).ToHttpResult();

        if (project.Status != ProjectStatus.Draft)
            return Result<ProjectDto>.Conflict(ProjectTextResponses.NotDraft).ToHttpResult();

        // Строгая валидация перед публикацией
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(project.Title))
            errors.Add(ProjectTextResponses.TitleRequired);
        if (string.IsNullOrWhiteSpace(project.Description) || project.Description.Length < 50)
            errors.Add(ProjectTextResponses.InvalidDescriptionMinLenght);
        if (project.BudgetMin <= 0 || project.BudgetMax <= 0)
            errors.Add(ProjectTextResponses.InvalidBudget);
        if (project.Deadline <= DateTime.UtcNow)
            errors.Add(ProjectTextResponses.InvalidDeadline);
        if (project.RequiredSkills.Count == 0)
            errors.Add(ProjectTextResponses.OneOrMoreRequiredSkills);

        if (errors.Count > 0)
            return Result<ProjectDto>.BadRequest(ProjectTextResponses.ValidationError, errors).ToHttpResult();

        project.Status = ProjectStatus.Open;
        project.PublishedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<ProjectDto>.Ok(ProjectDto.FromEntity(project), ProjectTextResponses.Published).ToHttpResult();
    }
}