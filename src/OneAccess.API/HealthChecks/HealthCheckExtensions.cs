using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneAccess.Infrastructure.Persistence;

namespace OneAccess.API.HealthChecks;

/// <summary>
/// Configures and maps detailed and shallow health check endpoints per OneAccess.md Section 15.
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddOneAccessHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("HealthChecks:Enabled") ?? true;
        if (!enabled) return services;

        services.AddHealthChecks()
            .AddDbContextCheck<OneAccessDbContext>("database");

        return services;
    }

    public static IEndpointRouteBuilder MapOneAccessHealthChecks(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("HealthChecks:Enabled") ?? true;
        if (!enabled) return app;

        var detailedPath = configuration["HealthChecks:DetailedPath"] ?? "/health";
        var livenessPath = configuration["HealthChecks:LivenessPath"] ?? "/api/health";

        // Detailed health check (internal ops / LB internal)
        app.MapHealthChecks(detailedPath, new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    duration = report.TotalDuration.ToString(),
                    entries = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString(),
                        exception = e.Value.Exception?.Message
                    })
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        // Shallow liveness/readiness probe (public-facing LB)
        app.MapHealthChecks(livenessPath, new HealthCheckOptions
        {
            Predicate = _ => false, // shallow: no dependency execution
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new { status = "Healthy" };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        return app;
    }
}
