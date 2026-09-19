using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Application.Features.Auth.Commands.Logout;
using OneAccess.Application.Features.Auth.Commands.RefreshToken;
using OneAccess.Application.Features.Auth.Queries.GetCurrentUser;
using OneAccess.Domain.Enums;
using InfrastructureCookieOptions = OneAccess.Infrastructure.Options.CookieOptions;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Authentication endpoints for login, session refresh, logout, and current user identity (OneAccess.md Section 10 & 14).
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
            [FromBody] LoginCommand command,
            HttpContext httpContext,
            ISender sender,
            IOptions<InfrastructureCookieOptions> cookieOptions,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded || result.Value == null)
            {
                return Results.BadRequest(result.Error);
            }

            var user = result.Value.User;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("sub", user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("name", user.Username),
                new(ClaimTypes.Email, user.Email),
                new("email", user.Email)
            };

            if (user.IsSystemAdministrator)
            {
                claims.Add(new Claim(ClaimTypes.Role, SystemRoles.SystemAdministrator));
                claims.Add(new Claim("role", SystemRoles.SystemAdministrator));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(cookieOptions.Value.ExpiryMinutes),
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            return Results.Ok(user);
        })
        .AllowAnonymous()
        .RequireRateLimiting("Login")
        .WithName("Login");

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenCommand command,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireRateLimiting("Refresh")
        .WithName("RefreshToken");

        group.MapPost("/logout", async (
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new LogoutCommand(), ct);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Logged out successfully" });
        })
        .RequireAuthorization()
        .WithName("Logout");

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCurrentUserQuery(), ct);
            return result.Succeeded ? Results.Ok(result.Value) : Results.Unauthorized();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");

        return app;
    }
}

