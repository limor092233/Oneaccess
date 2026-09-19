using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface ISubSystemApi
{
    Task<IReadOnlyList<SubSystemDto>?> GetSubSystemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SubSystemDto>?> GetMySubSystemsAsync(CancellationToken ct = default);
    Task<RegisterSubSystemResponse?> RegisterSubSystemAsync(CreateSubSystemRequest request, CancellationToken ct = default);
    Task UpdateSubSystemAsync(Guid id, UpdateSubSystemRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SubSystemDto>?> GetRoleSubSystemsAsync(Guid roleId, CancellationToken ct = default);
    Task AssignRoleSubSystemAsync(Guid roleId, Guid subSystemId, CancellationToken ct = default);
    Task RevokeRoleSubSystemAsync(Guid roleId, Guid subSystemId, CancellationToken ct = default);
    Task<IReadOnlyList<SubSystemDto>?> GetUserSubSystemsAsync(Guid userId, CancellationToken ct = default);
    Task AssignUserSubSystemAsync(Guid userId, Guid subSystemId, CancellationToken ct = default);
    Task RevokeUserSubSystemAsync(Guid userId, Guid subSystemId, CancellationToken ct = default);
}

public class SubSystemApi : ISubSystemApi
{
    private readonly HttpClient _http;

    public SubSystemApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<IReadOnlyList<SubSystemDto>?> GetSubSystemsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/subsystems", ct);
        return await HandleResponseAsync<IReadOnlyList<SubSystemDto>>(response, ct);
    }

    public async Task<IReadOnlyList<SubSystemDto>?> GetMySubSystemsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/subsystems/mine", ct);
        return await HandleResponseAsync<IReadOnlyList<SubSystemDto>>(response, ct);
    }

    public async Task<RegisterSubSystemResponse?> RegisterSubSystemAsync(CreateSubSystemRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/subsystems", request, ct);
        return await HandleResponseAsync<RegisterSubSystemResponse>(response, ct);
    }

    public async Task UpdateSubSystemAsync(Guid id, UpdateSubSystemRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/subsystems/{id}", request, ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task<IReadOnlyList<SubSystemDto>?> GetRoleSubSystemsAsync(Guid roleId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/roles/{roleId}/subsystems", ct);
        return await HandleResponseAsync<IReadOnlyList<SubSystemDto>>(response, ct);
    }

    public async Task AssignRoleSubSystemAsync(Guid roleId, Guid subSystemId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/roles/{roleId}/subsystems", new AssignRoleSubSystemRequest(subSystemId), ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task RevokeRoleSubSystemAsync(Guid roleId, Guid subSystemId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/roles/{roleId}/subsystems/{subSystemId}", ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task<IReadOnlyList<SubSystemDto>?> GetUserSubSystemsAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/users/{userId}/subsystems", ct);
        return await HandleResponseAsync<IReadOnlyList<SubSystemDto>>(response, ct);
    }

    public async Task AssignUserSubSystemAsync(Guid userId, Guid subSystemId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/users/{userId}/subsystems", new AssignUserSubSystemRequest(subSystemId), ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task RevokeUserSubSystemAsync(Guid userId, Guid subSystemId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/users/{userId}/subsystems/{subSystemId}", ct);
        await HandleResponseAsync(response, ct);
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

    private static async Task HandleResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
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
