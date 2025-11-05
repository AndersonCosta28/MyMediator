using Mert1s.MyMediator;
using Mert1s.MyMediator.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public class AddMyMediatorIntegrationTests
{
    [Fact]
    public async Task Mediator_SendsRequest_ToRegisteredHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register handler explicitly to avoid picking up test-only conflict handlers from the assembly
        services.AddMyMediator(new[] { typeof(TestRequestHandler) });
        var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("Handled", result); // Verifica se o handler processou a requisição corretamente
    }

    [Fact]
    public async Task Mediator_ThrowsException_WhenNoHandlerRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMyMediator(new[] { typeof(TestRequestHandler) });
        var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new UnhandledRequest(), CancellationToken.None));

        Assert.Equal($"No handler found for type {typeof(UnhandledRequest).Name}.", exception.Message);
    }

    [Fact]
    public async Task Mediator_SendsRequest_ToRegisteredHandler_ByTypesOverload()
    {
        var services = new ServiceCollection();
        services.AddMyMediator(new[] { typeof(TestRequestHandler) });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestRequest(), CancellationToken.None);

        Assert.Equal("Handled", result);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllNotificationHandlers_WhenRegisteredByTypes()
    {
        var services = new ServiceCollection();
        services.AddMyMediator(new[] { typeof(NotificationHandlerA), typeof(NotificationHandlerB) });
        var provider = services.BuildServiceProvider();

        // Resolve IMediator then call PublishAsync on concrete implementation via dynamic
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new TestNotification();

        await ((dynamic)mediator).PublishAsync((dynamic)notification, CancellationToken.None);

        Assert.True(notification.HandledByA);
        Assert.True(notification.HandledByB);
    }

    [Fact]
    public void AddMyMediator_Throws_WhenMultipleRequestHandlersForSameRequestProvidedByTypes()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddMyMediator(new[] { typeof(ConflictHandlerA), typeof(ConflictHandlerB) }));

        Assert.Contains("Multiple handlers were found for request type", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Applies_PipelineBehaviors_RegisteredByTypes()
    {
        var services = new ServiceCollection();

        // Register behaviors explicitly to control registration order
        services.AddTransient<IPipelineBehavior<PipelineRequest, string>, BehaviorA>();
        services.AddTransient<IPipelineBehavior<PipelineRequest, string>, BehaviorB>();

        // Register handler via types overload
        services.AddMyMediator(new[] { typeof(PipelineRequestHandler) });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new PipelineRequest(), CancellationToken.None);

        // BehaviorA wraps BehaviorB wraps Handler -> "A" + "B" + "Base" + "B" + "A" => "ABBaseBA"
        Assert.Equal("ABBaseBA", result);
    }

    [Fact]
    public void Mediator_Is_Registered_As_Scoped()
    {
        var services = new ServiceCollection();
        services.AddMyMediator(new[] { typeof(TestRequestHandler) });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var m1a = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var m1b = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var m2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        Assert.Same(m1a, m1b);
        Assert.NotSame(m1a, m2);
    }

    // Classe de teste para simular um IRequest e IRequestHandler
    public class TestRequest : IRequest<string> { }

    public class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("Handled");
    }

    // Classe de teste para simular uma requisição sem handler
    private class UnhandledRequest : IRequest<string> { }

    // Notification test types
    public class TestNotification : INotification
    {
        public bool HandledByA { get; set; }
        public bool HandledByB { get; set; }
    }

    public class NotificationHandlerA : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            notification.HandledByA = true;
            return Task.CompletedTask;
        }
    }

    public class NotificationHandlerB : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            notification.HandledByB = true;
            return Task.CompletedTask;
        }
    }

    // Conflict test types
    public class ConflictRequest : IRequest<string> { }

    public class ConflictHandlerA : IRequestHandler<ConflictRequest, string>
    {
        public Task<string> HandleAsync(ConflictRequest request, CancellationToken cancellationToken) => Task.FromResult("A");
    }

    public class ConflictHandlerB : IRequestHandler<ConflictRequest, string>
    {
        public Task<string> HandleAsync(ConflictRequest request, CancellationToken cancellationToken) => Task.FromResult("B");
    }

    // Pipeline test types
    public class PipelineRequest : IRequest<string> { }

    public class PipelineRequestHandler : IRequestHandler<PipelineRequest, string>
    {
        public Task<string> HandleAsync(PipelineRequest request, CancellationToken cancellationToken) => Task.FromResult("Base");
    }

    public class BehaviorA : IPipelineBehavior<PipelineRequest, string>
    {
        public async Task<string> HandleAsync(PipelineRequest request, CancellationToken cancellationToken, System.Func<Task<string>> next)
        {
            var inner = await next();
            return "A" + inner + "A";
        }
    }

    public class BehaviorB : IPipelineBehavior<PipelineRequest, string>
    {
        public async Task<string> HandleAsync(PipelineRequest request, CancellationToken cancellationToken, System.Func<Task<string>> next)
        {
            var inner = await next();
            return "B" + inner + "B";
        }
    }
}