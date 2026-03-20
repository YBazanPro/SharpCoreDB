// <copyright file="ProjectionMetricsSample.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Metrics sample emitted for each projection run.
/// </summary>
/// <param name="ProjectionName">Projection name.</param>
/// <param name="DatabaseId">Database identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="ProcessedEvents">Number of events processed in this run.</param>
/// <param name="CheckpointSequence">Latest checkpoint sequence after the run.</param>
/// <param name="EstimatedLag">Estimated remaining event lag after the run.</param>
/// <param name="CheckpointAge">Elapsed time since checkpoint update.</param>
/// <param name="Duration">Run duration.</param>
/// <param name="Success">Indicates whether run completed successfully.</param>
/// <param name="RecordedAtUtc">Metrics sample timestamp.</param>
public readonly record struct ProjectionMetricsSample(
    string ProjectionName,
    string DatabaseId,
    string TenantId,
    int ProcessedEvents,
    long CheckpointSequence,
    long EstimatedLag,
    TimeSpan CheckpointAge,
    TimeSpan Duration,
    bool Success,
    DateTimeOffset RecordedAtUtc);
