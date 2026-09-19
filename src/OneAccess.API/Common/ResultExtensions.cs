using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneAccess.Application.Common.Models;

namespace OneAccess.API.Common;

/// <summary>
/// Extensions for converting CQRS Result objects to standardized RFC 7807 ProblemDetails HTTP responses.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, string? instance = null)
    {
        if (result.Succeeded)
        {
            return Results.Ok(new { message = "Success" });
        }

        return ToProblem(result.StatusCode, result.Error, result.ErrorCode, instance);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, string? instance = null)
    {
        if (result.Succeeded)
        {
            return Results.Ok(result.Value);
        }

        return ToProblem(result.StatusCode, result.Error, result.ErrorCode, instance);
    }

    public static IResult ToProblem(int statusCode, string? detail, string? errorCode = null, string? instance = null)
    {
        var extensions = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(errorCode))
        {
            extensions["errorCode"] = errorCode;
        }

        var title = statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _ => "An error occurred"
        };

        return Results.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title,
            instance: instance,
            extensions: extensions.Count > 0 ? extensions : null
        );
    }
}
