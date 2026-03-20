// <copyright file="IProjectionRunner.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Runs a projection over ordered events and tracks checkpoints.
/// </summary>
public interface IProjectionRunner
{
    /// <summary>
    /// Executes projection processing for a run request.
    /// </summary>
    /// <param name="eventStore">Event store source.</param>
    /// <param name="projection">Projection to execute.</param>
    /// <param name="request">Run request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run result details.</returns>
    Task<ProjectionRunResult> RunAsync(
        IEventStore eventStore,
        IProjection projection,
        ProjectionRunRequest request,
        CancellationToken cancellationToken = default);
}
