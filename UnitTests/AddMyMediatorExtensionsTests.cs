using Microsoft.Extensions.DependencyInjection;
using MyMediator;
using MyMediator.Extensions.DependencyInjection;
using MyMediator.Interfaces;
using Xunit;

namespace UnitTests;

public class AddMyMediatorExtensionsTests
{
    [Fact]
    public void AddMyMediator_RegistersHandlersAndMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMyMediator(typeof(AddMyMediatorExtensionsTests).Assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator); // Verifica se o IMediator foi registrado

        var handler = serviceProvider.GetService<IRequestHandler<TestRequest, string>>();
        Assert.NotNull(handler); // Verifica se o handler foi registrado
    }

    // Classe de teste para simular um IRequest e IRequestHandler
    private class TestRequest : IRequest<string> { }

    private class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Handled");
        }
    }
}