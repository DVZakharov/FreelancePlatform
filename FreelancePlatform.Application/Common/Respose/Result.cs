using Newtonsoft.Json;

namespace FreelancePlatform.Application.Common.Respose;

public class Result<T>
{
    public int Code { get; set; }
    public bool IsSuccess => Code is >= 200 and < 300;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public T? Value { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Errors { get; set; }


    public static Result<T> NotFound(string? message = null) =>
        new() { Code = 404, Message = message };
    public static Result<T> BadRequest(string? message = null, List<string>? errors = null) =>
        new() { Code = 400, Message = message, Errors = errors };
    public static Result<T> Conflict(string? message = null) =>
        new() { Code = 409, Message = message };


    public static Result<T> Unauthorized(string? message = null) =>
        new() { Code = 401, Message = message };
    public static Result<T> Forbidden(string? message = null) =>
        new() { Code = 403, Message = message };
    public static Result<T> InternalServerError(string? message = null) =>
        new() { Code = 500, Message = message };


    public static Result<T> Ok(T? value = default, string? message = null) =>
        new() { Code = 200, Value = value, Message = message };
    public static Result<T> Created(T? value = default, string? message = null) =>
        new() { Code = 201, Value = value, Message = message };
}