using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface IUserApi
{
    Task<PagedResult<UserDto>?> GetUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<UserDetailsDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task AssignRoleAsync(Guid id, Guid roleId, CancellationToken ct = default);
}

public class UserApi : IUserApi
{
    private readonly HttpClient _http;

    public UserApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<PagedResult<UserDto>?> GetUsersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
    {
        var url = $"/api/users?pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
        }

        var response = await _http.GetAsync(url, ct);
        return await HandleResponseAsync<PagedResult<UserDto>>(response, ct);
    }

    public async Task<UserDetailsDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/users/{id}", ct);
        return await HandleResponseAsync<UserDetailsDto>(response, ct);
    }

    public async Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/users", request, ct);
        return await HandleResponseAsync<CreateUserResponse>(response, ct);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/users/{id}", request, ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/users/{id}", ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task AssignRoleAsync(Guid id, Guid roleId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/users/{id}/roles", new AssignRoleRequest(roleId), ct);
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
