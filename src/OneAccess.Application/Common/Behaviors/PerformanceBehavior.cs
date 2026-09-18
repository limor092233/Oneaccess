using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior monitoring execution performance and logging warnings for long-running requests (> 500ms).
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";

            _logger.LogWarning("Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) by User {UserId}",
                requestName, elapsedMilliseconds, userId);
        }

        return response;
    }
}
