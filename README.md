# MyMediator

MyMediator is a lightweight implementation of the Mediator design pattern for .NET applications. It provides a simple and extensible way to decouple the communication between objects by using requests and handlers.

Supported target: `.NET9`.

## Features

- **Request/Response Pattern**: Supports sending requests and receiving responses.
- **Dependency Injection**: Easily integrates with Microsoft.Extensions.DependencyInjection.
- **Extensibility**: Add custom handlers and requests with minimal effort.
- **Unit and Integration Tests**: Includes comprehensive tests to ensure reliability.

## Installation

To use MyMediator in your project, clone this repository and add the `Src` folder to your solution. Alternatively, you can package it as a NuGet package and reference it in your projects.

## Usage

### Registering MyMediator

To register MyMediator in your application, use the provided extension method in your `Startup.cs` or `Program.cs`:

```csharp
using MyMediator.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMyMediator(typeof(Program).Assembly);
```

For top-level statements / .NET9 (no `Program` class), you can use:

```csharp
using System.Reflection;
using MyMediator.Extensions.DependencyInjection;

builder.Services.AddMyMediator(Assembly.GetExecutingAssembly());
```

(Use `builder.Services.AddMyMediator(...)` when using the generic host in minimal hosting.)

### Creating a Request and Handler

Define a request by implementing the `IRequest<TResponse>` interface:

```csharp
public class MyRequest : IRequest<string>
{
 public string Message { get; set; }
}
```

Create a handler by implementing the `IRequestHandler<TRequest, TResponse>` interface:

```csharp
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
 public Task<string> HandleAsync(MyRequest request, CancellationToken cancellationToken)
 {
 return Task.FromResult($"Handled: {request.Message}");
 }
}
```

### Sending a Request

Use the `IMediator` interface to send a request:

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
var response = await mediator.SendAsync(new MyRequest { Message = "Hello, Mediator!" });
Console.WriteLine(response); // Output: Handled: Hello, Mediator!
```

Minimal example using top-level statements (.NET9):

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MyMediator.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMyMediator(Assembly.GetExecutingAssembly());
var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
var response = await mediator.SendAsync(new MyRequest { Message = "Hello from minimal app" });
Console.WriteLine(response);
```

## Testing

### Unit Tests

Unit tests are located in the `UnitTests` project. To run them, use:

```bash
dotnet test ./Tests/UnitTests/UnitTests.csproj
```

### Integration Tests

Integration tests are located in the `IntegrationTests` project. To run them, use:

```bash
dotnet test ./Tests/IntegrationTests/IntegrationTests.csproj
```

## Packaging & Publishing

This repository includes a GitHub Actions workflow that builds, packs and (optionally) publishes the package to GitHub Packages: `.github/workflows/nuget-publish.yml`.

To create a local NuGet package:

```bash
dotnet pack --configuration Release --output ./nupkg
```

To push to a NuGet feed (example):

```bash
dotnet nuget push ./nupkg/*.nupkg --source <YOUR_FEED_URL> --api-key <API_KEY>
```

Note: the CI workflow targets `.NET9`. Ensure the corresponding SDK is installed locally or configured in CI.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to improve this project.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

This project was inspired by the Mediator design pattern and aims to provide a simple and effective implementation for .NET developers.