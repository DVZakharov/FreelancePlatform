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

namespace FreelancePlatform.Application.Features.Projects.UpdateProject;

public static class UpdateProjectEndpoint
{
    [Authorize]
    [WolverinePut("/api/v1/projects/{id}"), Tags("Projects")]
    [EndpointSummary("Редактирование проекта")]
    [EndpointDescription("Обновляет проект. Доступно только владельцу и только пока проект в статусе " +
        "Draft или Open. После начала работы (InProgress) редактирование запрещено.")]
    public static async Task<IResult> Handle(
        [FromRoute] string id,
        [FromBody] UpdateProjectRequest request,
        HttpContext ctx,
        IValidator<UpdateProjectRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProjectDto>.BadRequest(
                ProjectTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        if (!Guid.TryParse(id, out var projectId))
            return Result<ProjectDto>.BadRequest(ProjectTextResponses.ValidationError, [ProjectTextResponses.InvalidId]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();

        var project = await db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Result<ProjectDto>.NotFound(ProjectTextResponses.NotFound).ToHttpResult();

        if (project.ClientId != currentUserId)
            return Result<ProjectDto>.Forbidden(ProjectTextResponses.NotOwner).ToHttpResult();

        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.Open)
            return Result<ProjectDto>.Conflict(ProjectTextResponses.CannotEdit).ToHttpResult();

        var categoryId = Guid.Parse(request.CategoryId);
        var category = await db.Categories.FindAsync([categoryId], ct);
        if (category is null)
            return Result<ProjectDto>.NotFound(ProjectTextResponses.CategoryNotFound).ToHttpResult();

        var skillIds = request.SkillIds.Select(Guid.Parse).ToList();
        var skills = await db.Skills.Where(s => skillIds.Contains(s.Id)).ToListAsync(ct);
        if (skills.Count != skillIds.Distinct().Count())
            return Result<ProjectDto>.BadRequest(ProjectTextResponses.SkillsNotFound).ToHttpResult();

        project.CategoryId = categoryId;
        project.Category = category;
        project.Title = request.Title;
        project.Description = request.Description;
        project.BudgetType = Enum.Parse<BudgetType>(request.BudgetType, true);
        project.BudgetMin = request.BudgetMin;
        project.BudgetMax = request.BudgetMax;
        project.Deadline = request.Deadline;
        project.RequiredSkills = skills;

        await db.SaveChangesAsync(ct);

        return Result<ProjectDto>.Ok(ProjectDto.FromEntity(project), ProjectTextResponses.Updated).ToHttpResult();
    }
}