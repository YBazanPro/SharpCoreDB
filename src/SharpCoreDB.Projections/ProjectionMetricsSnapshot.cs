// <copyright file="ProjectionMetricsSnapshot.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Aggregated projection metrics for a projection scope.
/// </summary>
/// <param name="ProjectionName">Projection name.</param>
/// <param name="DatabaseId">Database identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="TotalRuns">Total number of runs recorded.</param>
/// <param name="TotalProcessedEvents">Total number of processed events across all runs.</param>
/// <param name="LastCheckpointSequence">Latest checkpoint sequence.</param>
/// <param name="LastEstimatedLag">Latest estimated lag.</param>
/// <param name="LastCheckpointAge">Latest checkpoint age.</param>
/// <param name="LastRunDuration">Latest run duration.</param>
/// <param name="LastRunSucceeded">Indicates whether latest run succeeded.</param>
/// <param name="LastRecordedAtUtc">Latest metrics sample timestamp.</param>
public readonly record struct ProjectionMetricsSnapshot(
    string ProjectionName,
    string DatabaseId,
    string TenantId,
    long TotalRuns,
    long TotalProcessedEvents,
    long LastCheckpointSequence,
    long LastEstimatedLag,
    TimeSpan LastCheckpointAge,
    TimeSpan LastRunDuration,
    bool LastRunSucceeded,
    DateTimeOffset LastRecordedAtUtc);
