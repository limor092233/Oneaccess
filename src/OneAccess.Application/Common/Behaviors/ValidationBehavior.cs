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
            
            // Check if TResponse is Result or Result<T>
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(errorMessage, "ValidationError", 400);
            }

            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(TResponse).GetMethod("Failure", new[] { typeof(string), typeof(string), typeof(int) });
                if (failureMethod != null)
                {
                    var result = failureMethod.Invoke(null, new object?[] { errorMessage, "ValidationError", 400 });
                    return (TResponse)result!;
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
