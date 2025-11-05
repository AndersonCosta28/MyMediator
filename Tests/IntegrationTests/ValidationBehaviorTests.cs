using Mert1s.MyMediator;
using Mert1s.MyMediator.Extensions.DependencyInjection;
using Mert1s.MyValidator;
using Mert1s.MyValidator.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IntegrationTests;

public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    public class ValidRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) => Task.FromResult("OK");
    }

    public class InvalidRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult("ShouldNotReach");
    }

    public class ValidRequestValidator : ValidatorBuilder<TestRequest>
    {
        public ValidRequestValidator() => this.RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task ValidationBehavior_Allows_ValidRequest()
    {
        var services = new ServiceCollection();

        // Register pipeline behavior explicitly in front of handler
        services.AddPipelineBehaviors(typeof(ValidationBehavior<TestRequest, string>));

        services.AddMyValidator(Assembly.GetExecutingAssembly());

        services.AddMyMediator([typeof(ValidRequestHandler)]);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestRequest("name"), CancellationToken.None);

        Assert.Equal("OK", result);
    }

    [Fact]
    public async Task ValidationBehavior_Throws_On_InvalidRequest()
    {
        var services = new ServiceCollection();

        // Register ValidationBehavior in the pipeline
        services.AddPipelineBehaviors(typeof(ValidationBehavior<,>));
        services.AddMyValidator(Assembly.GetExecutingAssembly());
        services.AddMyMediator([typeof(InvalidRequestHandler)]);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<Mert1s.MyValidator.ValidationException>(() => mediator.SendAsync(new TestRequest(""), CancellationToken.None));
    }
}
