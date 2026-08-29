using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Soenneker.Cosmos.Client.Abstract;

/// <summary>
/// Should be used for all Cosmos access. Handles disposal of the client.
/// </summary>
public interface ICosmosClientUtil : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Returns the configured cosmos Client used by the cosmos client.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested cosmos Client.</returns>
    [Pure]
    ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the configured cosmos Client used by the cosmos client.
    /// </summary>
    /// <param name="endpoint">Service endpoint to call.</param>
    /// <param name="accountKey">Account key used for authentication.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested cosmos Client.</returns>
    [Pure]
    ValueTask<CosmosClient> Get(string endpoint, string accountKey, CancellationToken cancellationToken = default);
}
