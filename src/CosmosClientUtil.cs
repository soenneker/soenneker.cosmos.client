using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Serializer;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.Configuration;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Client;

///<inheritdoc cref="ICosmosClientUtil"/>
public class CosmosClientUtil : ICosmosClientUtil
{
    private readonly ILogger<CosmosClientUtil> _logger;

    private AsyncSingleton<CosmosClient>? _client;
    private AsyncSingleton<HttpClient>? _httpClient;

    private string? _endpoint;
    private string? _accountKey;
    private string? _environment;
    private bool _requestResponseLog;

    private bool _disposed;

    public CosmosClientUtil(IConfiguration config, IMemoryStreamUtil memoryStreamUtil, ILogger<CosmosClientUtil> logger)
    {
        _logger = logger;

        SetConfiguration(config);

        SetHttpClientInitialization();

        SetCosmosClientInitialization(memoryStreamUtil);
    }

    private void SetCosmosClientInitialization(IMemoryStreamUtil memoryStreamUtil)
    {
        _client = new AsyncSingleton<CosmosClient>(() =>
        {
            _logger.LogInformation("Initializing Cosmos client using endpoint: {endpoint}", _endpoint);

            // TODO: move to one serializer instance
            CosmosClientOptions clientOptions = new()
            {
                ConnectionMode = GetConnectionMode(),
                Serializer = new CosmosSystemTextJsonSerializer(memoryStreamUtil),
                HttpClientFactory = _httpClient!.GetSync
            };

            var client = new CosmosClient(_endpoint, _accountKey, clientOptions);

            ConfigureRequestResponseLogging();

            _logger.LogInformation("Finished initializing Cosmos client using endpoint: {endpoint}", _endpoint);

            return client;
        });
    }

    private void SetHttpClientInitialization()
    {
        _httpClient = new AsyncSingleton<HttpClient>(() =>
        {
            HttpClient httpClient;

            if (_environment == DeployEnvironment.Local.Name || _environment == DeployEnvironment.Test.Name)
            {
                _logger.LogWarning("Dangerously accepting any server certificate for Cosmos!");

                const int timeoutSecs = 120;

                _logger.LogDebug("Setting timeout for Cosmos to {timeout}s", timeoutSecs);

                var testHttpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                httpClient = new HttpClient(testHttpClientHandler)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSecs)
                };
            }
            else
            {
                var socketsHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
                };

                httpClient = new HttpClient(socketsHandler);
            }

            return httpClient;
        });
    }

    private void SetConfiguration(IConfiguration config)
    {
        _endpoint = config.GetValueStrict<string>("Azure:Cosmos:Endpoint");
        _accountKey = config.GetValueStrict<string>("Azure:Cosmos:AccountKey");
        _environment = config.GetValueStrict<string>("Environment");

        _requestResponseLog = config.GetValue<bool>("Azure:Cosmos:RequestResponseLog");
    }

    public ValueTask<CosmosClient> GetClient()
    {
        return _client!.Get();
    }

    private ConnectionMode GetConnectionMode()
    {
        if (_environment == DeployEnvironment.Test.Name)
            return ConnectionMode.Gateway;

        return ConnectionMode.Direct;
    }
    
    // https://github.com/Azure/azure-cosmos-dotnet-v3/issues/892
    private void ConfigureRequestResponseLogging()
    {
        if (!_requestResponseLog)
            return;

        var defaultTrace = Type.GetType("Microsoft.Azure.Cosmos.Core.Trace.DefaultTrace,Microsoft.Azure.Cosmos.Direct");
        var traceSource = defaultTrace?.GetProperty("TraceSource")?.GetValue(null) as TraceSource;

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
        if (_disposed)
        {
            _logger.LogWarning("-- COSMOS: There was an attempt to re-dispose the Cosmos client!");
            return;
        }

        GC.SuppressFinalize(this);

        _disposed = true;

        _logger.LogDebug("-- COSMOS: Disposing...");

        if (_httpClient != null)
            await _httpClient.DisposeAsync();

        if (_client != null)
            await _client.DisposeAsync();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            _logger.LogWarning("-- COSMOS: There was an attempt to re-dispose the Cosmos client!");
            return;
        }

        GC.SuppressFinalize(this);

        _disposed = true;

        _logger.LogDebug("-- COSMOS: Disposing...");

        _httpClient?.Dispose();
        _client?.Dispose();
    }
}