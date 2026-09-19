using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using OneAccess.Client.Models;

namespace OneAccess.Client.Services.Auth;

/// <summary>
/// Authentication state provider leveraging the BFF's httpOnly session cookie.
/// Calls GET /api/auth/me on initialization and builds a ClaimsPrincipal with role and permission claims.
/// </summary>
public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CookieAuthenticationStateProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OneAccessApi");
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/auth/me");
            if (response.StatusCode == HttpStatusCode.Unauthorized || !response.IsSuccessStatusCode)
            {
                return AnonymousState;
            }

            var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
            if (user == null)
            {
                return AnonymousState;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("sub", user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("name", user.Username),
                new(ClaimTypes.Email, user.Email),
                new("email", user.Email),
                new("fullName", user.FullName)
            };

            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                    claims.Add(new Claim("role", role));
                }
            }

            if (user.IsSystemAdministrator)
            {
                if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "System Administrator"))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "System Administrator"));
                    claims.Add(new Claim("role", "System Administrator"));
                }
            }

            if (user.Permissions != null)
            {
                foreach (var permission in user.Permissions)
                {
                    claims.Add(new Claim("permission", permission));
                }
            }

            var identity = new ClaimsIdentity(claims, "OneAccessCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            return AnonymousState;
        }
    }

    public void NotifyStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
