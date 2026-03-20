// <copyright file="InlineProjectionRunnerMetricsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for projection metrics emitted by <see cref="InlineProjectionRunner"/>.
/// </summary>
public class InlineProjectionRunnerMetricsTests
{
    [Fact]
    public async Task RunAsync_WithProcessedEvents_RecordsProcessedEventCountInMetrics()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var eventStore = new InMemoryEventStore();
        await eventStore.AppendEventsAsync(
            new EventStreamId("metrics-stream-1"),
            [
                new EventAppendEntry("E1", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
                new EventAppendEntry("E2", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow)
            ],
            cancellationToken);

        var metrics = new InMemoryProjectionMetrics();
        var runner = new InlineProjectionRunner(new InMemoryProjectionCheckpointStore(), metrics);
        await runner.RunAsync(
            eventStore,
            new MetricsProjection(),
            new ProjectionRunRequest("main", "tenant-a", 1, 100),
            cancellationToken);

        var snapshot = metrics.GetSnapshot(nameof(MetricsProjection), "main", "tenant-a");
        Assert.Equal(2, snapshot?.TotalProcessedEvents);
    }

    [Fact]
    public async Task RunAsync_WithPartialBatch_RecordsLagInMetrics()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var eventStore = new InMemoryEventStore();
        await eventStore.AppendEventsAsync(
            new EventStreamId("metrics-stream-2"),
            [
                new EventAppendEntry("E1", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
                new EventAppendEntry("E2", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow)
            ],
            cancellationToken);

        var metrics = new InMemoryProjectionMetrics();
        var runner = new InlineProjectionRunner(new InMemoryProjectionCheckpointStore(), metrics);
        await runner.RunAsync(
            eventStore,
            new MetricsProjection(),
            new ProjectionRunRequest("main", "tenant-a", 1, 1),
            cancellationToken);

        var snapshot = metrics.GetSnapshot(nameof(MetricsProjection), "main", "tenant-a");
        Assert.Equal(1, snapshot?.LastEstimatedLag);
    }

    private sealed class MetricsProjection : IProjection
    {
        public string Name => nameof(MetricsProjection);

        public Task ProjectAsync(
            EventEnvelope envelope,
            ProjectionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
