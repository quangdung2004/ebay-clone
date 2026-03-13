using Microsoft.Extensions.DependencyInjection;

namespace CloneEbay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-level services here as the project grows.
        // Currently, auth service interfaces are defined here but implemented
        // in Infrastructure and registered via AddInfrastructure().
        return services;
    }
}
