using FreelancePlatform.Application.Features.Categories.CreateCategory;
using Newtonsoft.Json;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.Newtonsoft;
using Wolverine.Newtonsoft;

namespace FreelancePlatform.Api.Extensions;

public static class WolverineExtensions
{
    public static void ConfigureWolverine(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();

            opts.UseNewtonsoftForSerialization(settings =>
            {
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            // Любой класс из сборки Application — достаточно для Discovery
            opts.Discovery.IncludeAssembly(typeof(CreateCategoryRequest).Assembly);
        });
    }

    public static IServiceCollection AddWolverineHttpServices(this IServiceCollection services)
    {
        services
            .AddWolverineHttp()
            .AddWolverineHttpNewtonsoft();

        return services;
    }

    public static void MapWolverineHttpEndpoints(this WebApplication app)
    {
        app.MapWolverineEndpoints(opts =>
        {
            opts.UseNewtonsoftJsonForSerialization(settings =>
            {
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });
        });
    }
}