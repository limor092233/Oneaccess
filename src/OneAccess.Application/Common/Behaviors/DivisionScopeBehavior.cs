using MediatR;
using Microsoft.Extensions.Logging;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior enforcing division scope authorization for non-System Administrators.
/// Intercepts IDivisionScopedRequest implementations before handler execution.
/// </summary>
public class DivisionScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<DivisionScopeBehavior<TRequest, TResponse>> _logger;

    public DivisionScopeBehavior(
        ICurrentUserService currentUserService,
        IReadDbContext readDbContext,
        ILogger<DivisionScopeBehavior<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IDivisionScopedRequest divisionScopedRequest)
        {
            return await next();
        }

        if (_currentUserService.IsSystemAdministrator)
        {
            return await next();
        }

        Guid? targetDivisionId;
        try
        {
            targetDivisionId = await divisionScopedRequest.GetTargetDivisionIdAsync(_readDbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve target division scope. Failing closed.");
            return ReturnFailure("Unable to verify division scope authorization.", "ServiceUnavailable", 503);
        }

        if (!targetDivisionId.HasValue)
        {
            return await next();
        }

        IReadOnlyList<Guid> assignedDivisionIds;
        try
        {
            assignedDivisionIds = await _currentUserService.GetAssignedDivisionIdsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assigned division IDs. Failing closed.");
            return ReturnFailure("Unable to verify division scope authorization.", "ServiceUnavailable", 503);
        }

        if (!assignedDivisionIds.Contains(targetDivisionId.Value))
        {
            _logger.LogWarning("User {UserId} denied access to out-of-scope division {DivisionId}",
                _currentUserService.UserId, targetDivisionId.Value);

            return ReturnFailure("You do not have permission to access resources in this division.", "Forbidden", 403);
        }

        return await next();
    }

    private static TResponse ReturnFailure(string error, string errorCode, int statusCode)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error, errorCode, statusCode);
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod("Failure", new[] { typeof(string), typeof(string), typeof(int) });
            if (failureMethod != null)
            {
                var result = failureMethod.Invoke(null, new object?[] { error, errorCode, statusCode });
                return (TResponse)result!;
            }
        }

        throw new UnauthorizedAccessException(error);
    }
}
