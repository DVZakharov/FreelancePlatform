using FreelancePlatform.Application.Common.Respose;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FreelancePlatform.Api.Extensions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (code, message) = exception switch
        {
            UnauthorizedAccessException => (401, "Unauthorized"),
            DbUpdateConcurrencyException => (409, "Данные изменены другим запросом, повторите"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (409, "Конфликтующие данные"),
            _ => (500, "An unexpected error occurred")
        };

        var errors = environment.IsDevelopment()
            ? new List<string> { exception.ToString() }
            : null;

        var result = new Result<object>
        {
            Code = code,
            Message = message,
            Errors = errors
        };

        httpContext.Response.StatusCode = code;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            Newtonsoft.Json.JsonConvert.SerializeObject(result),
            cancellationToken);

        return true;
    }
}
