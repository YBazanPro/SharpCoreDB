// <copyright file="InMemoryProjectionCheckpointStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe in-memory checkpoint store for local projection processing.
/// </summary>
public sealed class InMemoryProjectionCheckpointStore : IProjectionCheckpointStore
{
    private readonly ConcurrentDictionary<ProjectionCheckpointKey, ProjectionCheckpoint> _checkpoints = [];

    /// <inheritdoc />
    public Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionName,
        string databaseId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var key = new ProjectionCheckpointKey(projectionName, databaseId, tenantId);
        var found = _checkpoints.TryGetValue(key, out var checkpoint);
        return Task.FromResult(found ? checkpoint : (ProjectionCheckpoint?)null);
    }

    /// <inheritdoc />
    public Task SaveCheckpointAsync(
        ProjectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = new ProjectionCheckpointKey(checkpoint.ProjectionName, checkpoint.DatabaseId, checkpoint.TenantId);
        _checkpoints[key] = checkpoint;
        return Task.CompletedTask;
    }

    private readonly record struct ProjectionCheckpointKey(
        string ProjectionName,
        string DatabaseId,
        string TenantId);
}
