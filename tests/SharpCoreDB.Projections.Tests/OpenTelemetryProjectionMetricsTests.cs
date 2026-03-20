// <copyright file="OpenTelemetryProjectionMetricsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

/// <summary>
/// Unit tests for <see cref="OpenTelemetryProjectionMetrics"/>.
/// </summary>
public class OpenTelemetryProjectionMetricsTests
{
    [Fact]
    public void Record_WithValidSample_UpdatesInternalCounters()
    {
        using var metrics = new OpenTelemetryProjectionMetrics();

        metrics.Record(new ProjectionMetricsSample(
            ProjectionName: "OrdersProjection",
            DatabaseId: "main",
            TenantId: "tenant-a",
            ProcessedEvents: 5,
            CheckpointSequence: 42,
            EstimatedLag: 2,
            CheckpointAge: TimeSpan.FromMilliseconds(100),
            Duration: TimeSpan.FromMilliseconds(12),
            Success: true,
            RecordedAtUtc: DateTimeOffset.UtcNow));

        Assert.Equal(1, metrics.TotalSamples);
        Assert.Equal(5, metrics.TotalProcessedEvents);
    }
}
