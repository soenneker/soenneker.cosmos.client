using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Cosmos.Client.Abstract;

namespace Soenneker.Cosmos.Client.Registrars;

/// <summary>
/// A utility library for Azure Cosmos client accessibility
/// </summary>
public static class CosmosClientUtilRegistrar
{
    /// <summary>
    /// As Singleton
    /// </summary>
    public static void AddCosmosClientUtil(this IServiceCollection services)
    {
        services.TryAddSingleton<ICosmosClientUtil, CosmosClientUtil>();
    }
}