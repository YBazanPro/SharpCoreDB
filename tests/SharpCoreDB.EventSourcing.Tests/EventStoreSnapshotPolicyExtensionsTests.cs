// <copyright file="EventStoreSnapshotPolicyExtensionsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

using System.Text;

/// <summary>
/// Unit tests for snapshot policy extension helpers.
/// </summary>
public class EventStoreSnapshotPolicyExtensionsTests
{
    [Fact]
    public async Task ApplySnapshotPolicyAsync_WhenStreamLengthMatchesPolicy_SavesSnapshot()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("policy-1");
        await AppendManyAsync(store, streamId, 4);

        var created = await store.ApplySnapshotPolicyAsync(
            streamId,
            new SnapshotPolicy(EveryNEvents: 2),
            version => new EventSnapshot(streamId, version, Encoding.UTF8.GetBytes($"snapshot-{version}"), DateTimeOffset.UtcNow));

        var snapshot = await store.LoadSnapshotAsync(streamId);

        Assert.True(created);
        Assert.NotNull(snapshot);
        Assert.Equal(4, snapshot.Value.Version);
    }

    [Fact]
    public async Task ApplySnapshotPolicyAsync_WhenStreamLengthDoesNotMatchPolicy_DoesNotSaveSnapshot()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("policy-2");
        await AppendManyAsync(store, streamId, 3);

        var created = await store.ApplySnapshotPolicyAsync(
            streamId,
            new SnapshotPolicy(EveryNEvents: 5),
            version => new EventSnapshot(streamId, version, Encoding.UTF8.GetBytes($"snapshot-{version}"), DateTimeOffset.UtcNow));

        var snapshot = await store.LoadSnapshotAsync(streamId);

        Assert.False(created);
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task AppendEventsWithSnapshotPolicyAsync_WhenPolicyMatches_ReturnsSnapshotCreatedTrue()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("policy-3");

        var result = await store.AppendEventsWithSnapshotPolicyAsync(
            streamId,
            entries:
            [
                new EventAppendEntry("E1", ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
                new EventAppendEntry("E2", ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow)
            ],
            policy: new SnapshotPolicy(EveryNEvents: 2),
            snapshotFactory: version => new EventSnapshot(streamId, version, Encoding.UTF8.GetBytes($"snapshot-{version}"), DateTimeOffset.UtcNow));

        Assert.Equal(2, result.AppendResults.Count);
        Assert.True(result.SnapshotCreated);
    }

    private static async Task AppendManyAsync(IEventStore store, EventStreamId streamId, int count)
    {
        for (var index = 0; index < count; index++)
        {
            await store.AppendEventAsync(
                streamId,
                new EventAppendEntry("Event", ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow));
        }
    }
}
