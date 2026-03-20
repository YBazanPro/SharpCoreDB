// <copyright file="InMemoryCommandDispatcher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using System.Collections.Concurrent;

/// <summary>
/// In-memory command dispatcher using explicit handler registration.
/// </summary>
public sealed class InMemoryCommandDispatcher : ICommandDispatcher
{
    private readonly ConcurrentDictionary<Type, object> _handlers = [];

    /// <summary>
    /// Registers a command handler.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <param name="handler">Handler instance.</param>
    public void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[typeof(TCommand)] = handler;
    }

    /// <inheritdoc />
    public Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_handlers.TryGetValue(typeof(TCommand), out var handlerObject) || handlerObject is not ICommandHandler<TCommand> handler)
        {
            return Task.FromResult(CommandDispatchResult.Fail($"No handler registered for command '{typeof(TCommand).Name}'."));
        }

        return handler.HandleAsync(command, cancellationToken);
    }
}
