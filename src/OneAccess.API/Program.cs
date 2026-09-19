using OneAccess.API.Endpoints;
using OneAccess.API.Extensions;
using OneAccess.API.HealthChecks;
using OneAccess.API.Middleware;
using OneAccess.Application;
using OneAccess.Infrastructure;
using OneAccess.Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
builder.Host.UseSerilog((context, loggerConfig) =>
{
    SerilogConfigurator.Configure(loggerConfig, context.Configuration);
});

// 2. Register Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// 3. Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Middleware Pipeline in Strict Order (OneAccess.md Section 11)
// 1. CorrelationIdMiddleware
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. ExceptionHandlingMiddleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 3. Serilog Request Logging
app.UseSerilogRequestLogging();

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. CORS
app.UseCors();

// 6. Rate Limiting
app.UseRateLimiter();

// 7. Authentication
app.UseAuthentication();

// 8. Authorization
app.UseAuthorization();

// 9. SetupGuardMiddleware
app.UseMiddleware<SetupGuardMiddleware>();

// 10. Endpoints
app.MapSetupEndpoints();
app.MapAuthEndpoints();
app.MapJwksEndpoints();
app.MapOneAccessHealthChecks(builder.Configuration);

// 5. Initialize DB Seeding & First-Run Setup Check
await app.InitializeApplicationAsync();

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
