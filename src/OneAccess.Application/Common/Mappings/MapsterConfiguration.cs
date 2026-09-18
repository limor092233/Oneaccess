using Mapster;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Common.Mappings;

/// <summary>
/// Configures Mapster type adapters and explicit security ignores.
/// </summary>
public static class MapsterConfiguration
{
    public static void Configure()
    {
        // Global sensitive field exclusions across all mappings from entities
        TypeAdapterConfig<User, object>
            .NewConfig()
            .Ignore("PasswordHash");

        TypeAdapterConfig<RefreshToken, object>
            .NewConfig()
            .Ignore("TokenHash");

        TypeAdapterConfig<SetupCode, object>
            .NewConfig()
            .Ignore("CodeHash");

        // Scan the current assembly for IRegister configurations
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MapsterConfiguration).Assembly);
    }
}
