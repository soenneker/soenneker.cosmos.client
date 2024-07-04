using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Soenneker.Cosmos.Client.Abstract;

/// <summary>
/// Should be used for all Cosmos access. Handles disposal of the client.
/// Singleton IoC. Does not need disposal.
/// </summary>
public interface ICosmosClientUtil : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Implements a double locking mechanism for thread-safety while initial setup is happening.
    /// </summary>
    [Pure]
    ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default);
}