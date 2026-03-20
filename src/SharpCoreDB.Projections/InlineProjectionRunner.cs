// <copyright file="InlineProjectionRunner.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using System.Diagnostics;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Inline projection runner that processes one event batch and updates a checkpoint.
/// </summary>
public sealed class InlineProjectionRunner(
    IProjectionCheckpointStore checkpointStore,
    IProjectionMetrics? projectionMetrics = null) : IProjectionRunner
{
    private readonly IProjectionCheckpointStore _checkpointStore = checkpointStore ?? throw new ArgumentNullException(nameof(checkpointStore));
    private readonly IProjectionMetrics _projectionMetrics = projectionMetrics ?? NullProjectionMetrics.Instance;

    /// <inheritdoc />
    public async Task<ProjectionRunResult> RunAsync(
        IEventStore eventStore,
        IProjection projection,
        ProjectionRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);

        cancellationToken.ThrowIfCancellationRequested();
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var checkpoint = await _checkpointStore.GetCheckpointAsync(
            projection.Name,
            request.DatabaseId,
            request.TenantId,
            cancellationToken).ConfigureAwait(false);

        var fromSequence = Math.Max(
            request.FromGlobalSequence,
            checkpoint is { } found ? found.GlobalSequence + 1 : 1);

        var read = await eventStore.ReadAllAsync(fromSequence, request.BatchSize, cancellationToken).ConfigureAwait(false);
        if (read.Events.Count == 0)
        {
            stopwatch.Stop();
            var lastSequenceWithoutChanges = checkpoint?.GlobalSequence ?? fromSequence - 1;
            var checkpointAge = checkpoint is { } existingCheckpoint
                ? DateTimeOffset.UtcNow - existingCheckpoint.UpdatedAtUtc
                : TimeSpan.Zero;

            _projectionMetrics.Record(new ProjectionMetricsSample(
                projection.Name,
                request.DatabaseId,
                request.TenantId,
                ProcessedEvents: 0,
                CheckpointSequence: lastSequenceWithoutChanges,
                EstimatedLag: 0,
                CheckpointAge: checkpointAge,
                Duration: stopwatch.Elapsed,
                Success: true,
                RecordedAtUtc: startedAtUtc));

            return new ProjectionRunResult(ProcessedEvents: 0, LastGlobalSequence: lastSequenceWithoutChanges);
        }

        var context = new ProjectionExecutionContext(request.DatabaseId, request.TenantId);
        foreach (var envelope in read.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await projection.ProjectAsync(envelope, context, cancellationToken).ConfigureAwait(false);
        }

        var lastSequence = read.Events[^1].GlobalSequence;
        var savedAtUtc = DateTimeOffset.UtcNow;
        await _checkpointStore.SaveCheckpointAsync(
            new ProjectionCheckpoint(
                projection.Name,
                request.DatabaseId,
                request.TenantId,
                lastSequence,
                savedAtUtc),
            cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        var estimatedLag = Math.Max(0, read.TotalCount - read.Events.Count);
        _projectionMetrics.Record(new ProjectionMetricsSample(
            projection.Name,
            request.DatabaseId,
            request.TenantId,
            ProcessedEvents: read.Events.Count,
            CheckpointSequence: lastSequence,
            EstimatedLag: estimatedLag,
            CheckpointAge: TimeSpan.Zero,
            Duration: stopwatch.Elapsed,
            Success: true,
            RecordedAtUtc: startedAtUtc));

        return new ProjectionRunResult(read.Events.Count, lastSequence);
    }
}
