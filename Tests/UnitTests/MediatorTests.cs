using Microsoft.Extensions.DependencyInjection;
using MyMediator;

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
        services.AddTransient<IPipelineBehavior<TestRequest, string>, Behavior1>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>, Behavior2>();
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
}
