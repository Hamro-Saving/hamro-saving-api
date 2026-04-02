using FluentValidation;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;

namespace HamroSavings.Application.Abstractions.Behaviors;

internal sealed class ValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        if (!validators.Any())
        {
            return await innerHandler.Handle(command, cancellationToken);
        }

        var context = new ValidationContext<TCommand>(command);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .Distinct()
            .ToArray();

        if (errors.Length != 0)
        {
            var validationError = new ValidationError(errors);
            return Result.Failure<TResponse>(validationError.ToError());
        }

        return await innerHandler.Handle(command, cancellationToken);
    }
}

internal sealed class ValidationDecoratorNoResponse<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        if (!validators.Any())
        {
            return await innerHandler.Handle(command, cancellationToken);
        }

        var context = new ValidationContext<TCommand>(command);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .Distinct()
            .ToArray();

        if (errors.Length != 0)
        {
            var validationError = new ValidationError(errors);
            return Result.Failure(validationError.ToError());
        }

        return await innerHandler.Handle(command, cancellationToken);
    }
}
