using FluentValidation;
using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior running FluentValidation validators before request handlers execute.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var errorsDict = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            
            // Check if TResponse is Result or Result<T>
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.ValidationFailure(errorsDict, errorMessage);
            }

            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(TResponse).GetMethod("ValidationFailure", new[] { typeof(Dictionary<string, string[]>), typeof(string) });
                if (failureMethod != null)
                {
                    var result = failureMethod.Invoke(null, new object?[] { errorsDict, errorMessage });
                    return (TResponse)result!;
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
