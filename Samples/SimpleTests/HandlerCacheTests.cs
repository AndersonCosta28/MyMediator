using Mert1s.MyMediator;
using Mert1s.MyMediator.Caching;
using Mert1s.MyMediator.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace SimpleTests;

public class HandlerCacheTests
{
    [Fact]
    public async Task MemoryHandlerInvokerCache_Works_With_Mediator()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();

        services.AddMyMediator(sp => new MemoryHandlerInvokerCache(sp.GetRequiredService<IMemoryCache>()));
        services.AddMyMediator();

        // Register a handler directly
        services.AddTransient<IRequestHandler<SampleRequest, string>, SampleRequestHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new SampleRequest { Message = "hello" }, CancellationToken.None);

        Assert.Equal("Handled: hello", result);
    }

    public class SampleRequest : IRequest<string> { public string Message { get; set; } = string.Empty; }
    public class SampleRequestHandler : IRequestHandler<SampleRequest, string>
    {
        public Task<string> HandleAsync(SampleRequest request, CancellationToken cancellationToken) => Task.FromResult($"Handled: {request.Message}");
    }
}
