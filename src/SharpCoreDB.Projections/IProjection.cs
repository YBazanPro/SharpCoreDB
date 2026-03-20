// <copyright file="IProjection.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Represents a projection that consumes ordered event envelopes.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Gets a stable projection name used for checkpoints and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies an event envelope to the read model.
    /// </summary>
    /// <param name="envelope">Event envelope to project.</param>
    /// <param name="context">Projection execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the event is projected.</returns>
    Task ProjectAsync(
        EventEnvelope envelope,
        ProjectionExecutionContext context,
        CancellationToken cancellationToken = default);
}
