// <copyright file="IProjectionCheckpointStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Persists projection checkpoints for reliable catch-up.
/// </summary>
public interface IProjectionCheckpointStore
{
    /// <summary>
    /// Gets a checkpoint for projection/database/tenant scope.
    /// </summary>
    /// <param name="projectionName">Projection name.</param>
    /// <param name="databaseId">Database identifier.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The checkpoint if found; otherwise <see langword="null"/>.</returns>
    Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionName,
        string databaseId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a projection checkpoint.
    /// </summary>
    /// <param name="checkpoint">Checkpoint to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when checkpoint persistence finishes.</returns>
    Task SaveCheckpointAsync(
        ProjectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}
