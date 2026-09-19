using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Infrastructure.BackgroundJobs;
using OneAccess.Infrastructure.Identity;
using OneAccess.Infrastructure.Options;
using OneAccess.Infrastructure.Persistence;
using OneAccess.Infrastructure.Persistence.Interceptors;
using OneAccess.Infrastructure.Persistence.Repositories;
using OneAccess.Infrastructure.Services;

namespace OneAccess.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Strongly-typed Options with DataAnnotations validation on start
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BackgroundJobOptions>()
            .Bind(configuration.GetSection(BackgroundJobOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SetupOptions>()
            .Bind(configuration.GetSection(SetupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SubSystemOptions>()
            .Bind(configuration.GetSection(SubSystemOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CookieOptions>()
            .Bind(configuration.GetSection(CookieOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 2. Core Providers & Interceptors
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        // 3. Persistence (DbContext, IReadDbContext, Repositories, UnitOfWork)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing or empty. " +
                "A valid SQL Server connection string is required for OneAccess. " +
                "Configure 'ConnectionStrings:DefaultConnection' via appsettings.json, environment variables, or secrets.");
        }

        services.AddDbContext<OneAccessDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.AddInterceptors(interceptor);
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OneAccessDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<OneAccessDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 4. Redis Distributed Cache & Token Revocation
        var redisConn = configuration.GetSection("Redis")["ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = configuration.GetSection("Redis")["InstanceName"] ?? "OneAccess:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ITokenRevocationService, RedisTokenRevocationService>();

        // 5. Identity & Security Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<RsaKeyProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISetupCodeService, SetupCodeService>();
        services.AddScoped<ISubSystemAccessService, SubSystemAccessService>();

        // 6. Background Jobs
        services.AddHostedService<RefreshTokenCleanupJob>();
        services.AddHostedService<PermissionCacheWarmupJob>();

        services.AddHttpContextAccessor();

        return services;
    }
}
