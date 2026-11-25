using FluentValidation;
using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace FreelancePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}