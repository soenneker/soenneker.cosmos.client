using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Soenneker.Cosmos.Client.Abstract;

/// <summary>
/// Provides cached, application-owned Azure Cosmos DB clients.
/// </summary>
public interface ICosmosClientUtil : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Returns the client created from the configured endpoint and account key.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the cached Cosmos client.</returns>
    ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a client for the specified endpoint and account key.
    /// </summary>
    /// <param name="endpoint">Service endpoint to call.</param>
    /// <param name="accountKey">Account key used for authentication.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is cached by endpoint and account-key identity.</returns>
    ValueTask<CosmosClient> Get(string endpoint, string accountKey, CancellationToken cancellationToken = default);
}
