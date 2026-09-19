using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface IRoleApi
{
    Task<IReadOnlyList<RoleDto>?> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid id, CancellationToken ct = default);
    Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
}

public class RoleApi : IRoleApi
{
    private readonly HttpClient _http;

    public RoleApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<IReadOnlyList<RoleDto>?> GetRolesAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/roles", ct);
        return await HandleResponseAsync<IReadOnlyList<RoleDto>>(response, ct);
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/roles", request, ct);
        return await HandleResponseAsync<RoleDto>(response, ct);
    }

    public async Task UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/roles/{id}", request, ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task DeleteRoleAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/roles/{id}", ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/roles/{roleId}/permissions", new AssignPermissionRequest(permissionId), ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/roles/{roleId}/permissions/{permissionId}", ct);
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
