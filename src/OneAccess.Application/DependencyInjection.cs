using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.Application.Common.Behaviors;
using OneAccess.Application.Common.Mappings;

namespace OneAccess.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Mapster
        MapsterConfiguration.Configure();

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // MediatR + Pipeline Behaviors (in strict order: Logging -> Validation -> DivisionScope -> Performance)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(DivisionScopeBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        return services;
    }
}
