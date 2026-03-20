// <copyright file="ServiceProviderCommandDispatcher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Command dispatcher that resolves handlers from dependency injection.
/// </summary>
public sealed class ServiceProviderCommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
        {
            return Task.FromResult(CommandDispatchResult.Fail($"No handler registered for command '{typeof(TCommand).Name}'."));
        }

        return handler.HandleAsync(command, cancellationToken);
    }
}
