using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface IPermissionApi
{
    Task<IReadOnlyList<PermissionDto>?> GetPermissionsAsync(CancellationToken ct = default);
}

public class PermissionApi : IPermissionApi
{
    private readonly HttpClient _http;

    public PermissionApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<IReadOnlyList<PermissionDto>?> GetPermissionsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/permissions", ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionDto>>(cancellationToken: ct);
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



