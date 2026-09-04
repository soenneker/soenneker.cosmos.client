using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Atomics.ValueBools;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Serializer;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Hashing.Sha256;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.MemoryStream.Abstract;
using Soenneker.Dictionaries.Singletons;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Cosmos.Client;

/// <inheritdoc cref="ICosmosClientUtil"/>
public sealed class CosmosClientUtil : ICosmosClientUtil
{
    private static readonly Sha256HashingUtil _sha256 = new();

    private readonly ILogger<CosmosClientUtil> _logger;
    private readonly IHttpClientCache _httpClientCache;
    private readonly string _endpoint;
    private readonly string _accountKey;

    private readonly SingletonDictionary<CosmosClient, string, string> _clients;

    private readonly bool _allowBulkExecution;
    private readonly bool _allowInsecureServerCertificate;
    private readonly ConnectionMode _connectionMode;
    private readonly string _httpCachePrefix = $"cosmos:{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, byte> _httpCacheKeys = new(StringComparer.Ordinal);

    private ValueAtomicBool _disposed = new(false);

    private readonly CosmosSystemTextJsonSerializer _serializer;

    private static readonly TimeSpan _pooledLifetime = TimeSpan.FromMinutes(10);

    public CosmosClientUtil(IConfiguration config, IMemoryStreamUtil memoryStreamUtil, ILogger<CosmosClientUtil> logger,
        IHttpClientCache httpClientCache)
    {
        _logger = logger;
        _httpClientCache = httpClientCache;

        var environment = config.GetValueStrict<string>("Environment");
        _allowBulkExecution = config.GetValue<bool>("Azure:Cosmos:AllowBulkExecution");
        var connectionMode = config.GetValue<string>("Azure:Cosmos:ConnectionMode");

        _connectionMode = string.IsNullOrEmpty(connectionMode) ? ConnectionMode.Direct :
            connectionMode.EqualsIgnoreCase("Direct") ? ConnectionMode.Direct :
            connectionMode.EqualsIgnoreCase("Gateway") ? ConnectionMode.Gateway :
            throw new InvalidOperationException("Invalid Azure Cosmos connection mode specified");

        bool isTestEnvironment = environment == DeployEnvironment.Local.Name || environment == DeployEnvironment.Test.Name;
        _allowInsecureServerCertificate = config.GetValue<bool>("Azure:Cosmos:AllowInsecureServerCertificate");

        if (_allowInsecureServerCertificate && !isTestEnvironment)
            throw new InvalidOperationException("Insecure Cosmos server certificates can only be enabled in Local or Test environments.");

        _endpoint = config.GetValueStrict<string>("Azure:Cosmos:Endpoint");
        _accountKey = config.GetValueStrict<string>("Azure:Cosmos:AccountKey");

        _serializer = new CosmosSystemTextJsonSerializer(memoryStreamUtil);

        _clients = new SingletonDictionary<CosmosClient, string, string>(InitializeClient);

    }

    private async ValueTask<CosmosClient> InitializeClient(string key, string endpoint, string accountKey,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Cosmos client using endpoint: {endpoint}", endpoint);

        string httpKey = $"{_httpCachePrefix}:{endpoint}";
        _httpCacheKeys.TryAdd(httpKey, 0);

        HttpClient httpClient = await GetHttpClient(httpKey, cancellationToken).NoSync();

        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = _connectionMode,
            AllowBulkExecution = _allowBulkExecution,
            Serializer = _serializer,
            HttpClientFactory = () => httpClient
        };

        var client = new CosmosClient(endpoint, accountKey, clientOptions);

        _logger.LogInformation("Finished initializing Cosmos client using endpoint: {endpoint}", endpoint);

        return client;
    }

    private ValueTask<HttpClient> GetHttpClient(string key, CancellationToken cancellationToken)
    {
        // No closure: state passed explicitly + static lambda
        return _httpClientCache.Get(key,
            (allowInsecureServerCertificate: _allowInsecureServerCertificate, logger: _logger, pooledLifetime: _pooledLifetime), static state =>
            {
                HttpClientOptions httpClientOptions;

                if (state.allowInsecureServerCertificate)
                {
                    state.logger.LogWarning("Dangerously accepting any server certificate for Cosmos!");

                    const int timeoutSecs = 120;

                    state.logger.LogDebug("Setting timeout for Cosmos to {timeout}s", timeoutSecs);

                    httpClientOptions = new HttpClientOptions
                    {
                        Timeout = TimeSpan.FromSeconds(timeoutSecs),
                        PooledConnectionLifetime = state.pooledLifetime,
                        ModifyPrimaryHandler = static handler => handler.SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = static (_, _, _, _) => true
                        }
                    };
                }
                else
                {
                    httpClientOptions = new HttpClientOptions
                    {
                        PooledConnectionLifetime = state.pooledLifetime
                    };
                }

                return httpClientOptions;
            }, cancellationToken);
    }

    public ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default)
    {
        return _clients.Get(GetClientKey(_endpoint, _accountKey), _endpoint, _accountKey, cancellationToken);
    }

    public ValueTask<CosmosClient> Get(string endpoint, string accountKey,
        CancellationToken cancellationToken = default)
    {
        return _clients.Get(GetClientKey(endpoint, accountKey), endpoint, accountKey, cancellationToken);
    }

    private static string GetClientKey(string endpoint, string accountKey)
    {
        byte[] accountKeyHash = _sha256.Hash(Encoding.UTF8.GetBytes(accountKey));
        return endpoint + '|' + Convert.ToHexString(accountKeyHash);
    }

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        await _clients.DisposeAsync().NoSync();

        foreach (string httpKey in _httpCacheKeys.Keys)
            await _httpClientCache.Remove(httpKey).NoSync();
    }

    /// <summary>
    /// Releases resources used by the current instance.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        _clients.Dispose();

        foreach (string httpKey in _httpCacheKeys.Keys)
            _httpClientCache.RemoveSync(httpKey);
    }
}
