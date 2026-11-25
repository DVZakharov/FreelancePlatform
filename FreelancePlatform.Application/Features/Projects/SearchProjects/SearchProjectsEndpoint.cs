using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Projects.SearchProjects;

public static class SearchProjectsEndpoint
{
    [WolverineGet("/api/v1/projects"), Tags("Projects")]
    [EndpointSummary("Поиск проектов")]
    [EndpointDescription("Публичный поиск опубликованных проектов (статус Open) с фильтрами по категории, " +
        "требуемым навыкам (AND) и диапазону бюджета, с постраничной разбивкой.")]
    public static async Task<IResult> Handle(
        [FromQuery] SearchProjectsRequest request,
        IValidator<SearchProjectsRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SearchProjectsResponse>.BadRequest(
                ProjectTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var query = db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .Where(p => p.Status == ProjectStatus.Open)
            .AsQueryable();

        if (request.CategoryId is not null)
        {
            var categoryId = Guid.Parse(request.CategoryId);
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (request.SkillIds is { Count: > 0 })
        {
            foreach (var skillIdStr in request.SkillIds)
            {
                var skillId = Guid.Parse(skillIdStr);
                query = query.Where(p => p.RequiredSkills.Any(s => s.Id == skillId));
            }
        }

        if (request.BudgetMin.HasValue)
            query = query.Where(p => p.BudgetMax >= request.BudgetMin.Value);

        if (request.BudgetMax.HasValue)
            query = query.Where(p => p.BudgetMin <= request.BudgetMax.Value);

        var totalCount = await query.CountAsync(ct);

        var projects = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = projects.Select(ProjectDto.FromEntity).ToList();
        var response = new SearchProjectsResponse(request.PageNumber, request.PageSize, totalCount, dtos.Count, dtos);

        return Result<SearchProjectsResponse>.Ok(response, ProjectTextResponses.SearchSuccess).ToHttpResult();
    }
}