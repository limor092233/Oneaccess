using System.Net;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services;

/// <summary>
/// Exception carrying parsed RFC 7807 ProblemDetails from API responses.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ProblemDetails? Problem { get; }
    public string? RawContent { get; }

    public ApiException(HttpStatusCode statusCode, ProblemDetails? problem, string? rawContent)
        : base(problem?.Detail ?? problem?.Title ?? $"API request failed with status code {statusCode}")
    {
        StatusCode = statusCode;
        Problem = problem;
        RawContent = rawContent;
    }

    public string? GetFieldError(string fieldName)
    {
        if (Problem?.Errors == null) return null;

        foreach (var kvp in Problem.Errors)
        {
            if (string.Equals(kvp.Key, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Join("; ", kvp.Value);
            }
        }

        return null;
    }
}
