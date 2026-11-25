using FreelancePlatform.Application.Common;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace FreelancePlatform.Application.Features.Projects.GetMyProjects;

public static class GetMyProjectsEndpoint
{
    [Authorize]
    [WolverineGet("/api/v1/users/me/projects"), Tags("Projects")]
    [EndpointSummary("Мои проекты")]
    [EndpointDescription("Возвращает все проекты текущего пользователя как заказчика, включая черновики " +
        "и завершённые/отменённые — в отличие от публичного поиска, здесь виден полный список независимо от статуса.")]
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = ctx.GetCurrentUserId();

        var projects = await db.Projects
            .Include(p => p.Client)
            .Include(p => p.Category)
            .Include(p => p.RequiredSkills)
            .Where(p => p.ClientId == currentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var dtos = projects.Select(ProjectDto.FromEntity).ToList();

        return Result<List<ProjectDto>>.Ok(dtos, ProjectTextResponses.GetSuccess).ToHttpResult();
    }
}