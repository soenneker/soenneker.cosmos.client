using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.MemoryStream.Abstract;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Cosmos.Client;

///<inheritdoc cref="ICosmosClientUtil"/>
public sealed class CosmosClientUtil : ICosmosClientUtil
{
    private readonly ILogger<CosmosClientUtil> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientCache _httpClientCache;

    private readonly SingletonDictionary<CosmosClient, string, string> _clients;

    private readonly bool _requestResponseLog;
    private readonly bool _isTestEnvironment;
    private readonly string? _connectionMode;

    private ValueAtomicBool _disposed = new(false);

    public CosmosClientUtil(IConfiguration config, IMemoryStreamUtil memoryStreamUtil, ILogger<CosmosClientUtil> logger, IHttpClientCache httpClientCache)
    {
        _logger = logger;
        _config = config;
        IMemoryStreamUtil memoryStreamUtil1 = memoryStreamUtil;
        _httpClientCache = httpClientCache;

        var environment = config.GetValueStrict<string>("Environment");
        _requestResponseLog = config.GetValue<bool>("Azure:Cosmos:RequestResponseLog");
        _connectionMode = config.GetValue<string>("Azure:Cosmos:ConnectionMode");

        if (_connectionMode.IsNullOrEmpty())
            _connectionMode = "Direct";

        _isTestEnvironment = environment == DeployEnvironment.Local.Name || environment == DeployEnvironment.Test.Name;

        _clients = new SingletonDictionary<CosmosClient, string, string>(async (key, endpoint, accountKey) =>
        {
            _logger.LogInformation("Initializing Cosmos client using endpoint: {endpoint}", endpoint);

            HttpClient httpClient = await GetHttpClient(key, CancellationToken.None)
                .NoSync();

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = GetConnectionMode(),
                Serializer = new CosmosSystemTextJsonSerializer(memoryStreamUtil1),
                HttpClientFactory = () => httpClient
            };

            var client = new CosmosClient(endpoint, accountKey, clientOptions);

            ConfigureRequestResponseLogging();

            _logger.LogInformation("Finished initializing Cosmos client using endpoint: {endpoint}", endpoint);

            return client;
        });
    }

    private ValueTask<HttpClient> GetHttpClient(string key, CancellationToken cancellationToken)
    {
        return _httpClientCache.Get(key, () =>
        {
            HttpClientOptions httpClientOptions;

            if (_isTestEnvironment)
            {
                _logger.LogWarning("Dangerously accepting any server certificate for Cosmos!");

                const int timeoutSecs = 120;

                _logger.LogDebug("Setting timeout for Cosmos to {timeout}s", timeoutSecs);

                var testHttpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                httpClientOptions = new HttpClientOptions
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSecs),
                    HttpClientHandler = testHttpClientHandler
                };
            }
            else
            {
                httpClientOptions = new HttpClientOptions
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
                };
            }

            return httpClientOptions;
        }, cancellationToken);
    }

    public ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default)
    {
        var endpoint = _config.GetValueStrict<string>("Azure:Cosmos:Endpoint");
        var accountKey = _config.GetValueStrict<string>("Azure:Cosmos:AccountKey");

        var key = $"{endpoint}-{accountKey}";

        return _clients.Get(key, endpoint, accountKey, cancellationToken);
    }

    public ValueTask<CosmosClient> Get(string endpoint, string accountKey, CancellationToken cancellationToken = default)
    {
        var key = $"{endpoint}-{accountKey}";

        return _clients.Get(key, endpoint, accountKey, cancellationToken);
    }

    private ConnectionMode GetConnectionMode()
    {
        return _connectionMode switch
        {
            "Direct" => ConnectionMode.Direct,
            "Gateway" => ConnectionMode.Gateway,
            _ => throw new Exception("Invalid Azure Cosmos connection mode specified")
        };
    }

    // https://github.com/Azure/azure-cosmos-dotnet-v3/issues/892
    private void ConfigureRequestResponseLogging()
    {
        if (!_requestResponseLog)
            return;

        var defaultTrace = Type.GetType("Microsoft.Azure.Cosmos.Core.Trace.DefaultTrace,Microsoft.Azure.Cosmos.Direct");
        var traceSource = defaultTrace?.GetProperty("TraceSource")
                                      ?.GetValue(null) as TraceSource;

        if (traceSource != null)
        {
            traceSource.Switch.Level = SourceLevels.All;
            traceSource.Listeners.Clear();
            _logger.LogDebug("Turned Cosmos request/response logging off");
        }
        else
            _logger.LogError("Trace source was null, unable to turn request/response logging off");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        foreach (string keys in await _clients.GetKeys())
        {
            await _httpClientCache.Remove(keys)
                                  .NoSync();
        }

        await _clients.DisposeAsync()
                      .NoSync();
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        foreach (string key in _clients.GetKeysSync())
        {
            _httpClientCache.RemoveSync(key);
        }

        _clients.Dispose();
    }
}