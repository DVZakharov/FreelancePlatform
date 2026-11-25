using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Respose;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Features.Admin.GetPlatformStats;

public static class GetPlatformStatsEndpoint
{
    [Authorize(Roles = "Admin")]
    [WolverineGet("/api/v1/admin/stats"), Tags("Admin")]
    [EndpointSummary("Статистика платформы")]
    [EndpointDescription("Возвращает сводные показатели: количество пользователей, фрилансеров и админов, " +
        "распределение проектов и контрактов по статусам, число открытых споров, сумму средств в эскроу, " +
        "суммарный баланс пользователей и общий объём пополнений. Доступно только администраторам.")]
    public static async Task<IResult> Handle(
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var totalUsers = await db.Users.CountAsync(ct);
        var totalAdmins = await db.Users.CountAsync(u => (u.Role & UserRole.Admin) == UserRole.Admin, ct);
        var totalFreelancers = await db.FreelancerProfiles.CountAsync(ct);

        var projectsByStatus = (await db.Projects
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.Status.ToString(), x => x.Count);

        var contractsByStatus = (await db.Contracts
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.Status.ToString(), x => x.Count);

        var openDisputes = await db.Disputes.CountAsync(d => d.Status == DisputeStatus.Open, ct);

        var escrowHeld = await db.EscrowHolds
            .Where(e => e.Status == HoldStatus.Held)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var totalBalance = await db.Users.SumAsync(u => (decimal?)u.Balance, ct) ?? 0m;

        var totalToppedUp = await db.Transactions
            .Where(t => t.Type == TransactionType.TopUp)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var stats = new PlatformStatsDto(
            totalUsers, totalAdmins, totalFreelancers,
            projectsByStatus, contractsByStatus,
            openDisputes, escrowHeld, totalBalance, totalToppedUp);

        return Result<PlatformStatsDto>.Ok(stats, AdminTextResponses.StatsRetrieved).ToHttpResult();
    }
}