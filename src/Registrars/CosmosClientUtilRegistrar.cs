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
    public static IServiceCollection AddCosmosClientUtilAsSingleton(this IServiceCollection services)
    {
        services.AddHttpClientCacheAsSingleton()
                .AddMemoryStreamUtilAsSingleton()
                .TryAddSingleton<ICosmosClientUtil, CosmosClientUtil>();

        return services;
    }
}