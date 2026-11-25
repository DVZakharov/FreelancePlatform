using FluentValidation;
using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Projects.CreateProject;

public static class CreateProjectEndpoint
{
    [Authorize]
    [WolverinePost("/api/v1/projects"), Tags("Projects")]
    [EndpointSummary("Создание проекта (черновик)")]
    [EndpointDescription("Создаёт новый проект в статусе Draft. Проект не виден в публичном поиске, " +
        "пока не будет опубликован через отдельный эндпоинт. Валидация на этом этапе мягкая — " +
        "черновик можно сохранить с неполными данными.")]
    public static async Task<IResult> Handle(
        CreateProjectRequest request,
        HttpContext ctx,
        IValidator<CreateProjectRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<ProjectDto>.BadRequest(
                ProjectTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var currentUserId = ctx.GetCurrentUserId();
        var categoryId = Guid.Parse(request.CategoryId);

        var category = await db.Categories.FindAsync([categoryId], ct);
        if (category is null)
            return Result<ProjectDto>.NotFound(ProjectTextResponses.CategoryNotFound).ToHttpResult();

        var skillIds = request.SkillIds.Select(Guid.Parse).ToList();
        var skills = await db.Skills.Where(s => skillIds.Contains(s.Id)).ToListAsync(ct);
        if (skills.Count != skillIds.Distinct().Count())
            return Result<ProjectDto>.BadRequest(ProjectTextResponses.SkillsNotFound).ToHttpResult();

        var project = new ProjectEntity
        {
            ClientId = currentUserId,
            CategoryId = categoryId,
            Title = request.Title,
            Description = request.Description,
            BudgetType = Enum.Parse<BudgetType>(request.BudgetType, true),
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            Deadline = request.Deadline,
            Status = ProjectStatus.Draft,
            RequiredSkills = skills,
            Category = category
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        var reloaded = await db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .FirstAsync(p => p.Id == project.Id, ct);

        return Result<ProjectDto>.Created(ProjectDto.FromEntity(reloaded), ProjectTextResponses.Created).ToHttpResult();
    }
}