using System.Net.Http.Json;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Api;

public interface ISectionApi
{
    Task<CreateSectionResponse?> CreateSectionAsync(CreateSectionRequest request, CancellationToken ct = default);
    Task UpdateSectionAsync(Guid id, UpdateSectionRequest request, CancellationToken ct = default);
    Task DeleteSectionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SectionDto>?> GetSectionsByDivisionAsync(Guid divisionId, CancellationToken ct = default);
}

public class SectionApi : ISectionApi
{
    private readonly HttpClient _http;

    public SectionApi(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("OneAccessApi");
    }

    public async Task<CreateSectionResponse?> CreateSectionAsync(CreateSectionRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/sections", request, ct);
        return await HandleResponseAsync<CreateSectionResponse>(response, ct);
    }

    public async Task UpdateSectionAsync(Guid id, UpdateSectionRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/sections/{id}", request, ct);
        await HandleResponseAsync(response, ct);
    }

    public async Task DeleteSectionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/sections/{id}", ct);
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
