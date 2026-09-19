using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface IDivisionApi
{
    Task<IReadOnlyList<DivisionDto>?> GetDivisionsAsync(CancellationToken ct = default);
    Task<CreateDivisionResponse?> CreateDivisionAsync(CreateDivisionRequest request, CancellationToken ct = default);
    Task UpdateDivisionAsync(Guid id, UpdateDivisionRequest request, CancellationToken ct = default);
    Task DeleteDivisionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DivisionAdministratorDto>?> GetDivisionAdministratorsAsync(Guid divisionId, CancellationToken ct = default);
    Task AssignDivisionAdministratorAsync(Guid divisionId, Guid userId, CancellationToken ct = default);
    Task RevokeDivisionAdministratorAsync(Guid divisionId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SectionDto>?> GetSectionsByDivisionAsync(Guid divisionId, CancellationToken ct = default);
}

public class DivisionApi : IDivisionApi
{
    private readonly HttpClient _http;

    public DivisionApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<IReadOnlyList<DivisionDto>?> GetDivisionsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/divisions", ct);
        return await HandleResponseAsync<IReadOnlyList<DivisionDto>>(response, ct);
    }

    public async Task<CreateDivisionResponse?> CreateDivisionAsync(CreateDivisionRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/divisions", request, ct);
        return await HandleResponseAsync<CreateDivisionResponse>(response, ct);
    }

    public async Task UpdateDivisionAsync(Guid id, UpdateDivisionRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/divisions/{id}", request, ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task DeleteDivisionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/divisions/{id}", ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task<IReadOnlyList<DivisionAdministratorDto>?> GetDivisionAdministratorsAsync(Guid divisionId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/divisions/{divisionId}/users", ct);
        return await HandleResponseAsync<IReadOnlyList<DivisionAdministratorDto>>(response, ct);
    }

    public async Task AssignDivisionAdministratorAsync(Guid divisionId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/divisions/{divisionId}/users", new AssignUserDivisionRequest(userId), ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task RevokeDivisionAdministratorAsync(Guid divisionId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/divisions/{divisionId}/users/{userId}", ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task<IReadOnlyList<SectionDto>?> GetSectionsByDivisionAsync(Guid divisionId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/divisions/{divisionId}/sections", ct);
        return await HandleResponseAsync<IReadOnlyList<SectionDto>>(response, ct);
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
