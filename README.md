# MyMediator

MyMediator is a lightweight implementation of the Mediator design pattern for .NET applications. It provides a simple and extensible way to decouple the communication between objects by using requests and handlers.

Supported target: `.NET 9`.

## Features

- **Request/Response Pattern**: Supports sending requests and receiving responses.
- **Dependency Injection**: Easily integrates with Microsoft.Extensions.DependencyInjection.
- **Extensibility**: Add custom handlers and requests with minimal effort.
- **Unit and Integration Tests**: Includes comprehensive tests to ensure reliability.

## Installation

To use MyMediator in your project, add the `Src` folder to your solution or reference the project. The library targets .NET 9.

## Quick registration examples

These examples show the recommended DI-first registration patterns and the available overloads of `AddMyMediator`.

1) Default (scan calling assembly)

```csharp
var services = new ServiceCollection();
services.AddMyMediator(); // scans calling assembly for handlers and registers IMediator
```

2) Scan specific assemblies

```csharp
services.AddMyMediator(Assembly.GetExecutingAssembly(), typeof(SomeTypeInOtherAssembly).Assembly);
```

3) Register specific handler `Type`s (manual discovery)

```csharp
services.AddMyMediator(new[] { typeof(MyHandler1), typeof(MyHandler2) });
```

4) Configure advanced options (behavior type cache)

```csharp
services.AddMyMediator(options =>
{
    options.BehaviorTypeCacheFactory = () =>
        new ConcurrentDictionary<(Type, Type), Type>();
});
```

5) DI-first: register a custom `IHandlerInvokerCache` (recommended when using MemoryCache or bounded caches)

```csharp
services.AddMemoryCache(); // register IMemoryCache
services.AddMyMediator(sp => new Mert1s.MyMediator.Caching.MemoryHandlerInvokerCache(sp.GetRequiredService<IMemoryCache>()));
services.AddMyMediator(); // register mediator and scan handlers
```

6) Register a concrete `IHandlerInvokerCache` instance

```csharp
var cache = new Mert1s.MyMediator.Caching.MemoryHandlerInvokerCache(new MemoryCache(new MemoryCacheOptions()));
services.AddMyMediator(cache);
services.AddMyMediator();
```

7) Register cache and scan assemblies in one call

```csharp
services.AddMyMediator(sp => new Mert1s.MyMediator.Caching.MemoryHandlerInvokerCache(sp.GetRequiredService<IMemoryCache>()), Assembly.GetExecutingAssembly());
```

Notes
- If you do not register an `IHandlerInvokerCache`, MyMediator falls back to an internal default implementation (`DefaultHandlerInvokerConcurrentCache`) which provides a simple ConcurrentDictionary-backed cache.
- Prefer DI-first registration (`AddMyMediator(sp => ...)` or `AddMyMediator(cacheInstance)`) because it follows standard .NET conventions and allows integrating services from the same `IServiceCollection`.

## Minimal end-to-end example

This minimal example shows a full request/handler, registration using `MemoryHandlerInvokerCache` and sending a request.

```csharp
using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Mert1s.MyMediator;
using Mert1s.MyMediator.Caching;

// Request + Handler
public record HelloRequest(string Name) : IRequest<string>;
public class HelloHandler : IRequestHandler<HelloRequest, string>
{
    public Task<string> HandleAsync(HelloRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"Hello, {request.Name}!");
}

// Program bootstrap
var services = new ServiceCollection();
services.AddMemoryCache();

// Register Memory-backed handler-invoker cache (DI-first) and MyMediator
services.AddMyMediator(sp => new MemoryHandlerInvokerCache(sp.GetRequiredService<IMemoryCache>()));
services.AddMyMediator(Assembly.GetExecutingAssembly());

// Register handler explicitly (or rely on assembly scanning)
services.AddTransient<IRequestHandler<HelloRequest, string>, HelloHandler>();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

var result = await mediator.SendAsync(new HelloRequest("World"), CancellationToken.None);
Console.WriteLine(result); // prints: Hello, World!
```

## Samples and tests

- A minimal sample project is available under `Samples/MinimalApp` (shows registration snippets). You can run it with:

```bash
cd Samples/MinimalApp
dotnet run
```

- A simple test project demonstrating `MemoryHandlerInvokerCache` is available at `Samples/SimpleTests`. Run its tests with:

```bash
dotnet test Samples/SimpleTests/SimpleTests.csproj
```

## Contributing and packaging

See the project README root for testing, packaging and CI details.