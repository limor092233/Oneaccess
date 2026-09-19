namespace OneAccess.Application.Common.Models;

/// <summary>
/// Represents the outcome of an application operation.
/// </summary>
public class Result
{
    public bool Succeeded { get; protected set; }
    public string? Error { get; protected set; }
    public string? ErrorCode { get; protected set; }
    public int StatusCode { get; protected set; } = 200;

    public Dictionary<string, string[]>? Errors { get; protected set; }

    protected Result(bool succeeded, string? error = null, string? errorCode = null, int statusCode = 200, Dictionary<string, string[]>? errors = null)
    {
        Succeeded = succeeded;
        Error = error;
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Errors = errors;
    }

    public static Result Success() => new(true);
    
    public static Result Failure(string error, string? errorCode = null, int statusCode = 400) 
        => new(false, error, errorCode, statusCode);

    public static Result ValidationFailure(Dictionary<string, string[]> errors, string? error = null)
        => new(false, error ?? "One or more validation errors occurred.", "ValidationError", 400, errors);

    public static Result NotFound(string error = "Resource not found.") 
        => new(false, error, "NotFound", 404);

    public static Result Forbidden(string error = "Access is forbidden.") 
        => new(false, error, "Forbidden", 403);

    public static Result Unauthorized(string error = "Unauthorized.") 
        => new(false, error, "Unauthorized", 401);

    public static Result Conflict(string error = "Conflict occurred.") 
        => new(false, error, "Conflict", 409);
}

/// <summary>
/// Represents the typed outcome of an application operation.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    protected Result(bool succeeded, T? value, string? error = null, string? errorCode = null, int statusCode = 200, Dictionary<string, string[]>? errors = null)
        : base(succeeded, error, errorCode, statusCode, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, statusCode: 200);

    public static new Result<T> Failure(string error, string? errorCode = null, int statusCode = 400) 
        => new(false, default, error, errorCode, statusCode);

    public static new Result<T> ValidationFailure(Dictionary<string, string[]> errors, string? error = null)
        => new(false, default, error ?? "One or more validation errors occurred.", "ValidationError", 400, errors);

    public static new Result<T> NotFound(string error = "Resource not found.") 
        => new(false, default, error, "NotFound", 404);

    public static new Result<T> Forbidden(string error = "Access is forbidden.") 
        => new(false, default, error, "Forbidden", 403);

    public static new Result<T> Unauthorized(string error = "Unauthorized.") 
        => new(false, default, error, "Unauthorized", 401);

    public static new Result<T> Conflict(string error = "Conflict occurred.") 
        => new(false, default, error, "Conflict", 409);
}
