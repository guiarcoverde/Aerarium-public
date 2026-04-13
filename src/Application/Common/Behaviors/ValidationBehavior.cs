namespace Aerarium.Application.Common.Behaviors;

using Aerarium.Domain.Common;
using FluentValidation;
using Mediator;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(message, cancellationToken);

        var context = new ValidationContext<TMessage>(message);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (errors.Count == 0)
            return await next(message, cancellationToken);

        var errorMessage = string.Join("; ", errors.Select(e => e.ErrorMessage));

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse);
            var failureMethod = resultType.GetMethod(nameof(Result<object>.Failure))!;
            return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
        }

        throw new ValidationException(errors);
    }
}
