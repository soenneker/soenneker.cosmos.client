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
    [Pure]
    ValueTask<CosmosClient> Get(string endpoint, string accountKey, CancellationToken cancellationToken = default);
}