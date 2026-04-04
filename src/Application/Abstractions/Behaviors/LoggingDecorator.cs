using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.SharedKernel;
using Microsoft.Extensions.Logging;

namespace HamroSavings.Application.Abstractions.Behaviors;

internal sealed class LoggingDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    ILogger<LoggingDecorator<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        string commandName = typeof(TCommand).Name;
        logger.LogInformation("Executing command {CommandName}", commandName);

        Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Command {CommandName} processed successfully", commandName);
        }
        else
        {
            logger.LogWarning("Command {CommandName} processed with error: {Error}", commandName, result.Error);
        }

        return result;
    }
}

internal sealed class LoggingDecoratorNoResponse<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    ILogger<LoggingDecoratorNoResponse<TCommand>> logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        string commandName = typeof(TCommand).Name;
        logger.LogInformation("Executing command {CommandName}", commandName);

        Result result = await innerHandler.Handle(command, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Command {CommandName} processed successfully", commandName);
        }
        else
        {
            logger.LogWarning("Command {CommandName} processed with error: {Error}", commandName, result.Error);
        }

        return result;
    }
}
