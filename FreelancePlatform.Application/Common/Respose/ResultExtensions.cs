using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Application.Common.Respose;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return Results.Json(result, statusCode: result.Code);
    }
}
