using Microsoft.AspNetCore.Http;
using FreelancePlatform.Application.Common.Respose;

namespace FreelancePlatform.Tests.Helpers;

public static class ResultAssert
{
    // Results.Json(...) возвращает IValueHttpResult, достаём из него Result<T>
    public static Result<T> Unwrap<T>(IResult response)
    {
        var value = Assert.IsAssignableFrom<IValueHttpResult>(response).Value;
        return Assert.IsType<Result<T>>(value);
    }
}