using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OneAccess.API.Authorization;
using OneAccess.API.HealthChecks;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Infrastructure.Options;
using OneAccess.Infrastructure.Persistence;
using OneAccess.Infrastructure.Persistence.Seed;

using InfrastructureCookieOptions = OneAccess.Infrastructure.Options.CookieOptions;

namespace OneAccess.API.Extensions;

/// <summary>
/// Service and pipeline configuration extensions for OneAccess.API.
/// </summary>
public static class WebApplicationExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // 1. Cookie Authentication (Standard ASP.NET Core session cookie)
        var cookieSection = configuration.GetSection(InfrastructureCookieOptions.SectionName);
        var cookieName = cookieSection["Name"] ?? "oneaccess.session";
        var expiryMinutes = configuration.GetValue<int?>("Cookie:ExpiryMinutes") ?? 480;
        var configuredSecure = configuration.GetValue<bool?>("Cookie:Secure") ?? true;

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = cookieName;
                options.Cookie.HttpOnly = true;
                // Enforce SecurePolicy.Always in non-development environments or when explicitly configured
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? (configuredSecure ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None)
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(expiryMinutes);
                options.SlidingExpiration = true;

                // API responses must never redirect to HTML login pages
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, OneAccessAuthorizationMiddlewareResultHandler>();

        // 2. CORS configuration from appsettings.json
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Length != 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true); // Local development fallback
                }

                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // 3. Rate Limiting configuration from appsettings.json
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

            // Login partition
            var loginLimit = configuration.GetValue<int?>("RateLimiting:Login:PermitLimit") ?? 5;
            var loginWindow = configuration.GetValue<int?>("RateLimiting:Login:WindowSeconds") ?? 60;
            options.AddFixedWindowLimiter("Login", opt =>
            {
                opt.PermitLimit = loginLimit;
                opt.Window = TimeSpan.FromSeconds(loginWindow);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Setup partition
            var setupLimit = configuration.GetValue<int?>("RateLimiting:Setup:PermitLimit") ?? 5;
            var setupWindow = configuration.GetValue<int?>("RateLimiting:Setup:WindowSeconds") ?? 300;
            options.AddFixedWindowLimiter("Setup", opt =>
            {
                opt.PermitLimit = setupLimit;
                opt.Window = TimeSpan.FromSeconds(setupWindow);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Refresh partition
            var refreshLimit = configuration.GetValue<int?>("RateLimiting:Refresh:PermitLimit") ?? 10;
            var refreshWindow = configuration.GetValue<int?>("RateLimiting:Refresh:WindowSeconds") ?? 60;
            options.AddFixedWindowLimiter("Refresh", opt =>
            {
                opt.PermitLimit = refreshLimit;
                opt.Window = TimeSpan.FromSeconds(refreshWindow);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        // 4. Health Checks
        services.AddOneAccessHealthChecks(configuration);

        // 5. OpenAPI / Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    /// <summary>
    /// Ensures database migration / seeding and triggers first-run setup code generation on app startup.
    /// </summary>
    public static async Task InitializeApplicationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            var dbContext = services.GetRequiredService<OneAccessDbContext>();
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Seed default permissions and roles
            await PermissionSeeder.SeedAsync(dbContext);

            // First-run setup check
            var setupCodeService = services.GetRequiredService<ISetupCodeService>();
            if (await setupCodeService.IsSetupRequiredAsync())
            {
                await setupCodeService.GenerateAndStoreCodeAsync();
            }
            else
            {
                logger.LogInformation("OneAccess initialized: System Administrator already configured.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during application initialization/seeding.");
        }
    }
}
