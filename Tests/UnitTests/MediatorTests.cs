using Mert1s.MyMediator;
using Mert1s.MyMediator.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace UnitTests;

public class MediatorTests
{
    [Fact]
    public async Task SendAsync_Generic_ReturnsResult_WhenHandlerRegistered()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var result = await mediator.SendAsync(new TestRequest(), CancellationToken.None);

        Assert.Equal("Handled", result);
    }

    [Fact]
    public async Task SendAsync_Generic_Throws_WhenNoHandlerRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        mediator.SendAsync(new UnhandledRequest(), CancellationToken.None));

        Assert.Equal($"No handler found for type {typeof(UnhandledRequest).Name}.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_InvokesHandler()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestVoidRequest>, TestVoidRequestHandler>();
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var request = new TestVoidRequest();

        await mediator.SendAsync(request, CancellationToken.None);

        Assert.True(request.Handled);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_Throws_WhenNoHandlerRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        mediator.SendAsync(new TestVoidRequest(), CancellationToken.None));

        Assert.Equal($"No handler found for type {typeof(TestVoidRequest).Name}.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Generic_Applies_Behaviors_InRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        // register pipeline behaviors using the extension method to preserve order
        services.AddPipelineBehaviors(typeof(Behavior1), typeof(Behavior2));
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var result = await mediator.SendAsync(new TestRequest(), CancellationToken.None);

        // Behavior1 wraps Behavior2 which wraps handler: expected "1" + "2" + "Handled" + "2" + "1" => "12Handled21"
        Assert.Equal("12Handled21", result);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllNotificationHandlers()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, NotificationHandlerA>();
        services.AddTransient<INotificationHandler<TestNotification>, NotificationHandlerB>();
        var provider = services.BuildServiceProvider();

        var mediator = new Mediator(provider);

        var notification = new TestNotification();

        await mediator.PublishAsync(notification, CancellationToken.None);

        Assert.True(notification.HandledByA);
        Assert.True(notification.HandledByB);
    }

    [Fact]
    public async Task SendAsync_Generic_Throws_OnNullRequest()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.SendAsync<string>((IRequest<string>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_NonGeneric_Throws_OnNullRequest()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.SendAsync((IRequest)null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_Throws_OnNullNotification()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.PublishAsync<TestNotification>(null!, CancellationToken.None));
    }

    // Concurrency / load tests
    [Fact]
    public async Task SendAsync_Generic_Concurrent_ProducesExpectedResults()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        var tasks = Enumerable.Range(0, 200).Select(_ => mediator.SendAsync(new TestRequest(), CancellationToken.None)).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal("Handled", r));
    }

    [Fact]
    public async Task SendAsync_NonGeneric_Concurrent_HandlesAllRequests()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestVoidRequest>, TestVoidRequestHandler>();
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        var requests = Enumerable.Range(0, 200).Select(_ => new TestVoidRequest()).ToArray();
        var tasks = requests.Select(r => mediator.SendAsync(r, CancellationToken.None)).ToArray();

        await Task.WhenAll(tasks);

        Assert.All(requests, r => Assert.True(r.Handled));
    }

    [Fact]
    public void AddMyMediator_WithOptions_Uses_HandlerInvokerCacheFactory()
    {
        var services = new ServiceCollection();

        var factoryCalled = false;

        services.AddMyMediator(sp =>
        {
            factoryCalled = true;
            return new TestHandlerInvokerCache();
        });

        services.AddMyMediator();

        // register a simple handler to avoid errors when resolving mediator
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();

        // Resolve mediator to force options usage
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.True(factoryCalled, "HandlerInvokerCacheFactory should have been invoked during mediator construction.");
    }

    [Fact]
    public void AddMyMediator_WithOptions_Uses_BehaviorTypeCacheFactory()
    {
        var services = new ServiceCollection();

        var factoryCalled = false;

        services.AddMyMediator(options =>
        {
            options.BehaviorTypeCacheFactory = () =>
            {
                factoryCalled = true;
                return new System.Collections.Concurrent.ConcurrentDictionary<(System.Type, System.Type), System.Type>();
            };
        });

        // register a simple handler to avoid errors when resolving mediator
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();

        // Resolve mediator to force options usage
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.True(factoryCalled, "BehaviorTypeCacheFactory should have been invoked during mediator construction.");
    }

    [Fact]
    public async Task AddMyMediator_WithMemoryHandlerInvokerCache_UsesMemoryImplementation()
    {
        var services = new ServiceCollection();

        var factoryCalled = false;

        services.AddMyMediator(sp =>
        {
            factoryCalled = true;
            // create MemoryCache instance directly for the test
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            return new Mert1s.MyMediator.Caching.MemoryHandlerInvokerCache(memoryCache);
        });

        services.AddMyMediator();

        // register a simple handler
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();

        // Resolve mediator and send a request to ensure invoker created and used
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestRequest(), CancellationToken.None);

        Assert.True(factoryCalled, "MemoryHandlerInvokerCache factory should have been invoked during mediator construction.");
        Assert.Equal("Handled", result);
    }

    // Test types
    public class TestRequest : IRequest<string> { }
    public class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) => Task.FromResult("Handled");
    }

    public class TestVoidRequest : IRequest
    {
        public bool Handled { get; set; }
    }

    public class TestVoidRequestHandler : IRequestHandler<TestVoidRequest>
    {
        public Task HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
        {
            request.Handled = true;
            return Task.CompletedTask;
        }
    }

    public class Behavior1 : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken, System.Func<Task<string>> next)
        {
            var inner = await next();
            return "1" + inner + "1";
        }
    }

    public class Behavior2 : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken, System.Func<Task<string>> next)
        {
            var inner = await next();
            return "2" + inner + "2";
        }
    }

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

    private class UnhandledRequest : IRequest<string> { }

    // Test-only handler invoker cache implementation used by unit tests
    private class TestHandlerInvokerCache : IHandlerInvokerCache
    {
        public Lazy<Func<object, CancellationToken, Task<object?>>> GetOrAdd((Type RequestType, Type ResponseType) key, Func<Lazy<Func<object, CancellationToken, Task<object?>>>> valueFactory)
        {
            return valueFactory();
        }
    }
}
