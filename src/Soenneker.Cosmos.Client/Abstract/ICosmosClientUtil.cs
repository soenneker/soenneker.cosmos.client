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
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    [Pure]
    ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="accountKey">The account key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    [Pure]
    ValueTask<CosmosClient> Get(string endpoint, string accountKey, CancellationToken cancellationToken = default);
}