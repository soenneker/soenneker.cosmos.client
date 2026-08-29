using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Utils.HttpClientCache.Registrar;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Cosmos.Client.Registrars;

/// <summary>
/// A utility library for Azure Cosmos client accessibility
/// </summary>
public static class CosmosClientUtilRegistrar
{
    /// <summary>
    /// Registers Cosmos Client Util with a singleton lifetime.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddCosmosClientUtilAsSingleton(this IServiceCollection services)
    {
        services.AddHttpClientCacheAsSingleton()
                .AddMemoryStreamUtilAsSingleton()
                .TryAddSingleton<ICosmosClientUtil, CosmosClientUtil>();

        return services;
    }
}
