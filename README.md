[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Client.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Client/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.client/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.client/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Client.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Client/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.client/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.client/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Client

Should be used for all Cosmos access. Handles disposal of the client.

## Install

```bash
dotnet add package Soenneker.Cosmos.Client
```

## Quick start

```csharp
using Soenneker.Cosmos.Client.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddCosmosClientUtilAsSingleton();
```

Registers Cosmos Client Util with a singleton lifetime.

## What you get

- `ICosmosClientUtil` — Should be used for all Cosmos access. Handles disposal of the client.
- `CosmosClientUtilRegistrar` — A utility library for Azure Cosmos client accessibility.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `CosmosClientUtilRegistrar.AddCosmosClientUtilAsSingleton(services)` | Registers Cosmos Client Util with a singleton lifetime. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Dispose instances you own when their scope ends so held resources can be released.
