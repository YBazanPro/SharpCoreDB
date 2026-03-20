// <copyright file="ICommandDispatcher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Dispatches commands to registered handlers.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command to its handler.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <param name="command">Command instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dispatch result.</returns>
    Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}
