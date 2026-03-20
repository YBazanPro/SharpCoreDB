// <copyright file="ICommandHandler.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Handles a specific command type.
/// </summary>
/// <typeparam name="TCommand">Command type.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles a command.
    /// </summary>
    /// <param name="command">Command instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dispatch result.</returns>
    Task<CommandDispatchResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}
