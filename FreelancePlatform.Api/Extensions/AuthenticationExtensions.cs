using FreelancePlatform.Application.Common.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FreelancePlatform.Application.Common.Respose;
using Newtonsoft.Json;

namespace FreelancePlatform.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings are not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context => {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = Result<object>.Unauthorized("Unauthorized");
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                },
                OnForbidden = async context => {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var result = Result<object>.Forbidden("Forbidden");
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}