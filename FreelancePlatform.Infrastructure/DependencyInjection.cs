using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FreelancePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>("freelancedb");

        services.AddScoped<IApplicationDbContext, AppDbContext>();

        return services;
    }
}