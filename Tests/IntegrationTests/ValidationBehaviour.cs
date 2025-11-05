using Mert1s.MyMediator;
using Mert1s.MyValidator;

namespace IntegrationTests;
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<ValidatorBuilder<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<ValidatorBuilder<TRequest>> _validators = validators;

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next)
    {
        if (this._validators.Any())
        {
            var validationResults = await Task.WhenAll(this._validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            var failures = validationResults
                .SelectMany(x => x)
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}