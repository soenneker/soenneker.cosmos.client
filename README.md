[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Client.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Client/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.client/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.client/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Client.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Client/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.client/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.client/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Client

Creates, caches, and disposes Azure Cosmos DB SDK clients with shared HTTP transport and Soenneker's System.Text.Json serializer.

## Install

```bash
dotnet add package Soenneker.Cosmos.Client
```

## Configuration

```json
{
  "Environment": "Production",
  "Azure": {
    "Cosmos": {
      "Endpoint": "https://your-account.documents.azure.com:443/",
      "AccountKey": "your-account-key",
      "ConnectionMode": "Direct",
      "AllowBulkExecution": false,
      "AllowInsecureServerCertificate": false
    }
  }
}
```

`ConnectionMode` accepts `Direct` or `Gateway` and defaults to `Direct` when omitted. `AllowBulkExecution` is passed to `CosmosClientOptions`.

`AllowInsecureServerCertificate` defaults to `false`. It is accepted only when `Environment` is `Local` or `Test` and is intended solely for a local Cosmos emulator with an untrusted development certificate. Never enable it against a remote endpoint.

## Registration

```csharp
using Soenneker.Cosmos.Client.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCosmosClientUtilAsSingleton();
```

The registrar intentionally exposes only a singleton lifetime because the Cosmos SDK client is designed for long-lived reuse.

## Usage

```csharp
using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Client.Abstract;

public sealed class OrdersDatabase(ICosmosClientUtil clientUtil)
{
    public async ValueTask<Database> Get(CancellationToken cancellationToken)
    {
        CosmosClient client = await clientUtil.Get(cancellationToken);
        return client.GetDatabase("orders");
    }
}
```

For another account, use the explicit overload:

```csharp
CosmosClient client = await clientUtil.Get(endpoint, accountKey, cancellationToken);
```

Clients are cached by endpoint and a SHA-256 identity of the account key. Calling the overload with a rotated key creates a separate client without storing the raw key in the cache key. HTTP transports are reused per endpoint within the utility instance.

## Practical notes

- Do not dispose a returned `CosmosClient`; the utility owns all clients it returns. Dependency injection disposes the utility at application shutdown.
- Client creation is lazy. Configuration and SDK options are captured when a client is first requested.
- The utility no longer modifies Cosmos SDK global trace listeners. Configure diagnostic logging through the application's logging and Cosmos SDK options.
- Account keys are credentials. Keep them in a secret provider and redact authorization material and request diagnostics from logs.
- Cancellation can stop lazy initialization; it does not cancel or dispose a client that has already been created.
