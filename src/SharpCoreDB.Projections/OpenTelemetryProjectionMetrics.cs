// <copyright file="OpenTelemetryProjectionMetrics.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

/// <summary>
/// OpenTelemetry-ready projection metrics implementation backed by <see cref="Meter"/> instruments.
/// </summary>
public sealed class OpenTelemetryProjectionMetrics : IProjectionMetrics, IDisposable
{
    /// <summary>
    /// OpenTelemetry meter name for projection metrics.
    /// </summary>
    public const string MeterName = "SharpCoreDB.Projections";

    /// <summary>
    /// OpenTelemetry instrumentation version.
    /// </summary>
    public const string InstrumentationVersion = "1.5.0";

    private readonly Meter _meter;
    private readonly Counter<long> _projectionRunsCounter;
    private readonly Counter<long> _projectionProcessedEventsCounter;
    private readonly Counter<long> _projectionRunFailuresCounter;
    private readonly Histogram<double> _projectionRunDurationMsHistogram;
    private readonly Histogram<long> _projectionLagHistogram;
    private readonly Histogram<double> _projectionCheckpointAgeMsHistogram;

    private long _totalSamples;
    private long _totalProcessedEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryProjectionMetrics"/> class.
    /// </summary>
    /// <param name="meterName">Optional meter name override.</param>
    /// <param name="instrumentationVersion">Optional instrumentation version override.</param>
    public OpenTelemetryProjectionMetrics(
        string meterName = MeterName,
        string instrumentationVersion = InstrumentationVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(instrumentationVersion);

        _meter = new Meter(meterName, instrumentationVersion);
        _projectionRunsCounter = _meter.CreateCounter<long>(
            "sharpcoredb.projections.runs.total",
            unit: "{run}",
            description: "Total number of projection runs");

        _projectionProcessedEventsCounter = _meter.CreateCounter<long>(
            "sharpcoredb.projections.events_processed.total",
            unit: "{event}",
            description: "Total number of events processed by projections");

        _projectionRunFailuresCounter = _meter.CreateCounter<long>(
            "sharpcoredb.projections.runs.failed",
            unit: "{run}",
            description: "Total number of failed projection runs");

        _projectionRunDurationMsHistogram = _meter.CreateHistogram<double>(
            "sharpcoredb.projections.run.duration_ms",
            unit: "ms",
            description: "Projection run duration in milliseconds");

        _projectionLagHistogram = _meter.CreateHistogram<long>(
            "sharpcoredb.projections.lag.events",
            unit: "{event}",
            description: "Estimated projection lag in events");

        _projectionCheckpointAgeMsHistogram = _meter.CreateHistogram<double>(
            "sharpcoredb.projections.checkpoint.age_ms",
            unit: "ms",
            description: "Projection checkpoint age in milliseconds");
    }

    /// <summary>
    /// Gets the total number of recorded projection metric samples.
    /// </summary>
    public long TotalSamples => Interlocked.Read(ref _totalSamples);

    /// <summary>
    /// Gets the total number of processed events observed across recorded samples.
    /// </summary>
    public long TotalProcessedEvents => Interlocked.Read(ref _totalProcessedEvents);

    /// <inheritdoc />
    public void Record(ProjectionMetricsSample sample)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.ProjectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.DatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sample.TenantId);

        var tags = new TagList
        {
            { "projection", sample.ProjectionName },
            { "database", sample.DatabaseId },
            { "tenant", sample.TenantId },
        };

        _projectionRunsCounter.Add(1, tags);
        _projectionProcessedEventsCounter.Add(sample.ProcessedEvents, tags);
        _projectionRunDurationMsHistogram.Record(sample.Duration.TotalMilliseconds, tags);
        _projectionLagHistogram.Record(sample.EstimatedLag, tags);
        _projectionCheckpointAgeMsHistogram.Record(sample.CheckpointAge.TotalMilliseconds, tags);

        if (!sample.Success)
        {
            _projectionRunFailuresCounter.Add(1, tags);
        }

        Interlocked.Increment(ref _totalSamples);
        Interlocked.Add(ref _totalProcessedEvents, sample.ProcessedEvents);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
