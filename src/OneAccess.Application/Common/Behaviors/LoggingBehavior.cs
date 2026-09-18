using MediatR;
using Microsoft.Extensions.Logging;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior logging incoming request details and execution duration.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";

        _logger.LogInformation("Handling {RequestName} for User {UserId}", requestName, userId);

        var response = await next();

        _logger.LogInformation("Handled {RequestName} for User {UserId}", requestName, userId);

        return response;
    }
}
