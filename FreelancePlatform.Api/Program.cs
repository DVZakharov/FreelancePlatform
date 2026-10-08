using FreelancePlatform.Api.Extensions;
using FreelancePlatform.Application;
using FreelancePlatform.Infrastructure;
using FreelancePlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .ConfigureWolverine();

builder.Services
    .AddWolverineHttpServices()
    .AddSwaggerWithJwt()
    .AddJwtAuthentication(builder.Configuration)
    .AddGlobalExceptionHandling()
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseSwaggerWithUi();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapWolverineHttpEndpoints();

app.Run();