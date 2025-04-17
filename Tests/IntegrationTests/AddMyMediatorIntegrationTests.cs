using Microsoft.Extensions.DependencyInjection;
using MyMediator;
using MyMediator.Extensions.DependencyInjection;
using MyMediator.Interfaces;
using Xunit;

namespace IntegrationTests;

public class AddMyMediatorIntegrationTests
{
    [Fact]
    public async Task Mediator_SendsRequest_ToRegisteredHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMyMediator(typeof(AddMyMediatorIntegrationTests).Assembly);
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
        services.AddMyMediator([typeof(TestRequestHandler)]);
        var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new UnhandledRequest(), CancellationToken.None));

        Assert.Equal($"No handler found for type {typeof(UnhandledRequest).Name}.", exception.Message);
    }

    // Classe de teste para simular um IRequest e IRequestHandler
    public class TestRequest : IRequest<string> { }

    public class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Handled");
        }
    }

    // Classe de teste para simular uma requisição sem handler
    private class UnhandledRequest : IRequest<string> { }
}