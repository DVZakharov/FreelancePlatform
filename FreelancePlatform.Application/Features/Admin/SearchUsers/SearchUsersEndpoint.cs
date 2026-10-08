using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Application.Features.Admin.SearchUsers;

public static class SearchUsersEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverineGet("/api/v1/admin/users"), Tags("Admin")]
    [EndpointSummary("Список пользователей")]
    [EndpointDescription("Возвращает постраничный список пользователей. Поддерживает поиск по email и имени " +
        "(частичное совпадение, без учёта регистра) и фильтр по роли. Доступно только администраторам.")]
    public static async Task<IResult> Handle(
        [FromBody] SearchUsersRequest request,
        IValidator<SearchUsersRequest> validator,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<SearchUsersResponse>.BadRequest(
                AdminTextResponses.ValidationError,
                [.. validation.Errors.Select(e => e.ErrorMessage)]).ToHttpResult();

        var query = db.Users.Include(u => u.FreelancerProfile).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.FullName.ToLower().Contains(search));
        }

        if (request.Role is not null)
        {
            var role = Enum.Parse<UserRole>(request.Role, true);
            query = query.Where(u => (u.Role & role) == role);
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = users.Select(u => UserDto.FromEntity(u, u.FreelancerProfile is not null)).ToList();
        var response = new SearchUsersResponse(request.PageNumber, request.PageSize, totalCount, dtos.Count, dtos);

        return Result<SearchUsersResponse>.Ok(response, AdminTextResponses.UsersRetrieved).ToHttpResult();
    }
}