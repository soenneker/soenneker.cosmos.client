﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Serializer;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.HttpClientCache.Dtos;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Cosmos.Client;

///<inheritdoc cref="ICosmosClientUtil"/>
public class CosmosClientUtil : ICosmosClientUtil
{
    private readonly ILogger<CosmosClientUtil> _logger;
    private readonly IHttpClientCache _httpClientCache;

    private readonly AsyncSingleton<CosmosClient>? _client;

    private readonly string? _environment;
    private readonly bool _requestResponseLog;
    private readonly bool _isTestEnvironment;
    
    private bool _disposed;

    public CosmosClientUtil(IConfiguration config, IMemoryStreamUtil memoryStreamUtil, ILogger<CosmosClientUtil> logger, IHttpClientCache httpClientCache)
    {
        _logger = logger;
        _httpClientCache = httpClientCache;

        var endpoint = config.GetValueStrict<string>("Azure:Cosmos:Endpoint");
        var accountKey = config.GetValueStrict<string>("Azure:Cosmos:AccountKey");
        _environment = config.GetValueStrict<string>("Environment");
        _requestResponseLog = config.GetValue<bool>("Azure:Cosmos:RequestResponseLog");
        _isTestEnvironment = _environment == DeployEnvironment.Local.Name || _environment == DeployEnvironment.Test.Name;

        _client = new AsyncSingleton<CosmosClient>(async (cancellationToken, _) =>
        {
            _logger.LogInformation("Initializing Cosmos client using endpoint: {endpoint}", endpoint);

            HttpClient httpClient = await GetHttpClient(cancellationToken).NoSync();

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = GetConnectionMode(),
                Serializer = new CosmosSystemTextJsonSerializer(memoryStreamUtil),
                HttpClientFactory = () => httpClient
            };

            var client = new CosmosClient(endpoint, accountKey, clientOptions);

            ConfigureRequestResponseLogging();

            _logger.LogInformation("Finished initializing Cosmos client using endpoint: {endpoint}", endpoint);

            return client;
        });
    }

    private async ValueTask<HttpClient> GetHttpClient(CancellationToken cancellationToken)
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

        HttpClient httpClient = await _httpClientCache.Get(nameof(CosmosClientUtil), httpClientOptions, cancellationToken).NoSync();
        return httpClient;
    }

    public ValueTask<CosmosClient> Get(CancellationToken cancellationToken = default)
    {
        return _client!.Get(cancellationToken);
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
            return;

        _disposed = true;

        if (_client != null)
            await _client.DisposeAsync().NoSync();

        await _httpClientCache.Remove(nameof(CosmosClientUtil)).NoSync();

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _client?.Dispose();

        _httpClientCache.RemoveSync(nameof(CosmosClientUtil));

        GC.SuppressFinalize(this);
    }
}