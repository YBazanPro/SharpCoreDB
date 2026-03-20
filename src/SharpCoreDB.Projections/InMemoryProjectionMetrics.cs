// <copyright file="InMemoryProjectionMetrics.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe in-memory projection metrics collector.
/// </summary>
public sealed class InMemoryProjectionMetrics : IProjectionMetrics
{
    private readonly ConcurrentDictionary<ProjectionMetricsKey, ProjectionMetricsSnapshot> _snapshots = [];

    /// <inheritdoc />
    public void Record(ProjectionMetricsSample sample)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.ProjectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.DatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.TenantId);

        var key = new ProjectionMetricsKey(sample.ProjectionName, sample.DatabaseId, sample.TenantId);
        _snapshots.AddOrUpdate(
            key,
            _ => new ProjectionMetricsSnapshot(
                sample.ProjectionName,
                sample.DatabaseId,
                sample.TenantId,
                TotalRuns: 1,
                TotalProcessedEvents: sample.ProcessedEvents,
                LastCheckpointSequence: sample.CheckpointSequence,
                LastEstimatedLag: sample.EstimatedLag,
                LastCheckpointAge: sample.CheckpointAge,
                LastRunDuration: sample.Duration,
                LastRunSucceeded: sample.Success,
                LastRecordedAtUtc: sample.RecordedAtUtc),
            (_, previous) => previous with
            {
                TotalRuns = previous.TotalRuns + 1,
                TotalProcessedEvents = previous.TotalProcessedEvents + sample.ProcessedEvents,
                LastCheckpointSequence = sample.CheckpointSequence,
                LastEstimatedLag = sample.EstimatedLag,
                LastCheckpointAge = sample.CheckpointAge,
                LastRunDuration = sample.Duration,
                LastRunSucceeded = sample.Success,
                LastRecordedAtUtc = sample.RecordedAtUtc,
            });
    }

    /// <summary>
    /// Gets projection metrics snapshot by projection scope.
    /// </summary>
    /// <param name="projectionName">Projection name.</param>
    /// <param name="databaseId">Database identifier.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The latest snapshot if available; otherwise <see langword="null"/>.</returns>
    public ProjectionMetricsSnapshot? GetSnapshot(string projectionName, string databaseId, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return _snapshots.TryGetValue(new ProjectionMetricsKey(projectionName, databaseId, tenantId), out var snapshot)
            ? snapshot
            : null;
    }

    private readonly record struct ProjectionMetricsKey(
        string ProjectionName,
        string DatabaseId,
        string TenantId);
}
