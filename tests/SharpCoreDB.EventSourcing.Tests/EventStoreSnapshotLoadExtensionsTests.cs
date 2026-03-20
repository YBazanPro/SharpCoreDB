// <copyright file="EventStoreSnapshotLoadExtensionsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

using System.Text;

/// <summary>
/// Unit tests for snapshot-aware aggregate load extensions.
/// </summary>
public class EventStoreSnapshotLoadExtensionsTests
{
    [Fact]
    public async Task LoadWithSnapshotAsync_WithoutSnapshot_BuildsAggregateFromFullEventStream()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("agg-1");

        await AppendValueAsync(store, streamId, 10);
        await AppendValueAsync(store, streamId, 5);

        var result = await store.LoadWithSnapshotAsync(
            streamId,
            fromEvents: static events => new CounterAggregate(events.Sum(GetValue)),
            fromSnapshot: static snapshotBytes => new CounterAggregate(ParseInt(snapshotBytes)),
            replayFromSnapshot: static (aggregate, events) => aggregate with { Total = aggregate.Total + events.Sum(GetValue) });

        Assert.Equal(15, result.Aggregate.Total);
        Assert.Equal(2, result.Version);
        Assert.Equal(2, result.ReplayedEvents);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public async Task LoadWithSnapshotAsync_WithSnapshot_ReplaysOnlyEventsAfterSnapshotVersion()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("agg-2");

        await AppendValueAsync(store, streamId, 10);
        await AppendValueAsync(store, streamId, 20);
        await AppendValueAsync(store, streamId, 5);
        await AppendValueAsync(store, streamId, 3);

        await store.SaveSnapshotAsync(new EventSnapshot(
            streamId,
            Version: 2,
            SnapshotData: "30"u8.ToArray(),
            CreatedAtUtc: DateTimeOffset.UtcNow));

        var result = await store.LoadWithSnapshotAsync(
            streamId,
            fromEvents: static events => new CounterAggregate(events.Sum(GetValue)),
            fromSnapshot: static snapshotBytes => new CounterAggregate(ParseInt(snapshotBytes)),
            replayFromSnapshot: static (aggregate, events) => aggregate with { Total = aggregate.Total + events.Sum(GetValue) });

        Assert.Equal(38, result.Aggregate.Total);
        Assert.Equal(4, result.Version);
        Assert.Equal(2, result.ReplayedEvents);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(2, result.Snapshot.Value.Version);
    }

    private static async Task AppendValueAsync(IEventStore store, EventStreamId streamId, int value)
    {
        await store.AppendEventAsync(
            streamId,
            new EventAppendEntry(
                EventType: "ValueAdded",
                Payload: Encoding.UTF8.GetBytes(value.ToString()),
                Metadata: ReadOnlyMemory<byte>.Empty,
                TimestampUtc: DateTimeOffset.UtcNow));
    }

    private static int GetValue(EventEnvelope envelope) => ParseInt(envelope.Payload);

    private static int ParseInt(ReadOnlyMemory<byte> bytes) => int.Parse(Encoding.UTF8.GetString(bytes.Span));

    private readonly record struct CounterAggregate(int Total);
}
