// <copyright file="InlineProjectionRunnerTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="InlineProjectionRunner"/>.
/// </summary>
public class InlineProjectionRunnerTests
{
    [Fact]
    public async Task RunAsync_WithEvents_UpdatesCheckpointToLastGlobalSequence()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var eventStore = new InMemoryEventStore();
        var streamId = new EventStreamId("order-1");

        await eventStore.AppendEventsAsync(
            streamId,
            [
                new EventAppendEntry("OrderCreated", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
                new EventAppendEntry("OrderConfirmed", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow)
            ],
            cancellationToken);

        var checkpointStore = new InMemoryProjectionCheckpointStore();
        var runner = new InlineProjectionRunner(checkpointStore);

        await runner.RunAsync(
            eventStore,
            new CountingProjection(),
            new ProjectionRunRequest("main", "tenant-a", 1, 100),
            cancellationToken);

        var checkpoint = await checkpointStore.GetCheckpointAsync(nameof(CountingProjection), "main", "tenant-a", cancellationToken);
        Assert.Equal(2, checkpoint?.GlobalSequence);
    }

    private sealed class CountingProjection : IProjection
    {
        public string Name => nameof(CountingProjection);

        public Task ProjectAsync(EventEnvelope envelope, ProjectionExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
