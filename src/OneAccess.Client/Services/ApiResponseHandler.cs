using System.Net;
using Microsoft.AspNetCore.Components;

namespace OneAccess.Client.Services;

/// <summary>
/// Centralized DelegatingHandler for the OneAccessApi HttpClient.
/// Handles 401 (redirect to /login), 403 (redirect to /forbidden), and 429 (rate limit toast).
/// </summary>
public class ApiResponseHandler : DelegatingHandler
{
    private readonly NavigationManager _navigation;
    private readonly ToastService _toastService;

    public ApiResponseHandler(NavigationManager navigation, ToastService toastService)
    {
        _navigation = navigation;
        _toastService = toastService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Do not redirect on login or auth/me checks (auth provider handles those directly)
            if (!path.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("/api/auth/me", StringComparison.OrdinalIgnoreCase))
            {
                _navigation.NavigateTo("/login");
            }
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _navigation.NavigateTo("/forbidden");
        }
        else if (response.StatusCode == (HttpStatusCode)429)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
            var msg = retryAfter.HasValue
                ? $"Too many requests. Please retry in {retryAfter.Value:F0} seconds."
                : "Too many requests. Please slow down and try again shortly.";
            _toastService.ShowWarning(msg);
        }

        return response;
    }
}
