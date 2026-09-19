using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface IAuditApi
{
    Task<PagedResult<AuditLogDto>?> GetAuditLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        string? entityType = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default);
}

public class AuditApi : IAuditApi
{
    private readonly HttpClient _http;

    public AuditApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<PagedResult<AuditLogDto>?> GetAuditLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        string? entityType = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (userId.HasValue)
        {
            queryParams.Add($"userId={userId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            queryParams.Add($"entityType={Uri.EscapeDataString(entityType.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            queryParams.Add($"action={Uri.EscapeDataString(action.Trim())}");
        }

        if (fromUtc.HasValue)
        {
            queryParams.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("o"))}");
        }

        if (toUtc.HasValue)
        {
            queryParams.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("o"))}");
        }

        var url = $"/api/audit?{string.Join("&", queryParams)}";
        var response = await _http.GetAsync(url, ct);
        return await HandleResponseAsync<PagedResult<AuditLogDto>>(response, ct);
    }

    private static async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }

        ProblemDetails? problem = null;
        string? rawContent = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
        }
        catch
        {
            rawContent = await response.Content.ReadAsStringAsync(ct);
        }

        throw new ApiException(response.StatusCode, problem, rawContent);
    }
}
