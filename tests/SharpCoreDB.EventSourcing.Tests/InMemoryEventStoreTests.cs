// <copyright file="InMemoryEventStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

/// <summary>
/// Unit tests for InMemoryEventStore append-only and read semantics.
/// </summary>
public class InMemoryEventStoreTests
{
    [Fact]
    public async Task AppendEventAsync_WithValidEntry_IncrementsSequence()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entry = new EventAppendEntry(
            "TestEvent",
            "payload"u8.ToArray(),
            "metadata"u8.ToArray(),
            DateTimeOffset.UtcNow);

        var result1 = await store.AppendEventAsync(streamId, entry);
        var result2 = await store.AppendEventAsync(streamId, entry);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(1, result1.AppendedSequence);
        Assert.Equal(2, result2.AppendedSequence);
    }

    [Fact]
    public async Task AppendEventAsync_WithMultipleStreams_IndependentSequences()
    {
        var store = new InMemoryEventStore();
        var stream1 = new EventStreamId("stream-1");
        var stream2 = new EventStreamId("stream-2");
        var entry = new EventAppendEntry(
            "Event",
            "payload"u8.ToArray(),
            Array.Empty<byte>(),
            DateTimeOffset.UtcNow);

        var result1 = await store.AppendEventAsync(stream1, entry);
        var result2 = await store.AppendEventAsync(stream2, entry);

        Assert.Equal(1, result1.AppendedSequence);
        Assert.Equal(1, result2.AppendedSequence);
    }

    [Fact]
    public async Task AppendEventsAsync_WithBatch_AssignsSequencesInOrder()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entries = new[]
        {
            new EventAppendEntry("Event1", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow),
            new EventAppendEntry("Event2", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow),
            new EventAppendEntry("Event3", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow),
        };

        var results = await store.AppendEventsAsync(streamId, entries);

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].AppendedSequence);
        Assert.Equal(2, results[1].AppendedSequence);
        Assert.Equal(3, results[2].AppendedSequence);
    }

    [Fact]
    public async Task ReadStreamAsync_WithSequenceRange_ReturnEventsInRange()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entries = Enumerable.Range(1, 5)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
            .ToList();

        await store.AppendEventsAsync(streamId, entries);

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(2, 4));

        Assert.Equal(3, result.Events.Count);
        Assert.Equal(2, result.Events[0].Sequence);
        Assert.Equal(3, result.Events[1].Sequence);
        Assert.Equal(4, result.Events[2].Sequence);
    }

    [Fact]
    public async Task ReadStreamAsync_WithNonExistentStream_ReturnsEmpty()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("nonexistent");

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(1, 10));

        Assert.Empty(result.Events);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ReadAllAsync_WithGlobalOrdering_ReturnEventsInGlobalSequence()
    {
        var store = new InMemoryEventStore();
        var stream1 = new EventStreamId("stream-1");
        var stream2 = new EventStreamId("stream-2");

        var entry1 = new EventAppendEntry("E1", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow);
        var entry2 = new EventAppendEntry("E2", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow);

        await store.AppendEventAsync(stream1, entry1);
        await store.AppendEventAsync(stream2, entry2);

        var result = await store.ReadAllAsync(1, 100);

        Assert.Equal(2, result.Events.Count);
        Assert.Equal(1, result.Events[0].GlobalSequence);
        Assert.Equal(2, result.Events[1].GlobalSequence);
    }

    [Fact]
    public async Task GetStreamLengthAsync_WithExistingStream_ReturnHighestSequence()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entries = Enumerable.Range(1, 5)
            .Select(_ => new EventAppendEntry("Event", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
            .ToList();

        await store.AppendEventsAsync(streamId, entries);
        var length = await store.GetStreamLengthAsync(streamId);

        Assert.Equal(5, length);
    }

    [Fact]
    public async Task GetStreamLengthAsync_WithNonExistentStream_ReturnsZero()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("nonexistent");

        var length = await store.GetStreamLengthAsync(streamId);

        Assert.Equal(0, length);
    }

    [Fact]
    public async Task Concurrency_MultipleAppendsAreOrdered()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("concurrent-stream");
        var tasks = Enumerable.Range(1, 10)
            .Select(i => store.AppendEventAsync(
                streamId,
                new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var sequences = results.Select(r => r.AppendedSequence).OrderBy(x => x).ToList();
        Assert.Equal(Enumerable.Range(1, 10).Select(x => (long)x), sequences);
    }

    [Fact]
    public async Task AppendEventsAsync_WithEmptyBatch_ReturnsEmptyResults()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entries = Array.Empty<EventAppendEntry>();

        var results = await store.AppendEventsAsync(streamId, entries);

        Assert.Empty(results);
    }

    [Fact]
    public async Task AppendEventsAsync_WithLargeBatch_HandlesAllEvents()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entries = Enumerable.Range(1, 1000)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
            .ToList();

        var results = await store.AppendEventsAsync(streamId, entries);

        Assert.Equal(1000, results.Count);
        Assert.Equal(1, results[0].AppendedSequence);
        Assert.Equal(1000, results[999].AppendedSequence);
    }

    [Fact]
    public async Task GlobalSequence_IsMonotonicallyIncreasing()
    {
        var store = new InMemoryEventStore();
        var stream1 = new EventStreamId("stream-1");
        var stream2 = new EventStreamId("stream-2");
        var entry = new EventAppendEntry("Event", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow);

        var result1 = await store.AppendEventAsync(stream1, entry);
        var result2 = await store.AppendEventAsync(stream2, entry);
        var result3 = await store.AppendEventAsync(stream1, entry);

        Assert.Equal(1, result1.GlobalSequence);
        Assert.Equal(2, result2.GlobalSequence);
        Assert.Equal(3, result3.GlobalSequence);
    }

    [Fact]
    public async Task GlobalSequence_IsContiguousAfterConcurrentAppends()
    {
        var store = new InMemoryEventStore();
        var streams = Enumerable.Range(1, 5)
            .Select(i => new EventStreamId($"stream-{i}"))
            .ToList();

        var tasks = streams.SelectMany(stream =>
            Enumerable.Range(1, 10).Select(_ =>
                store.AppendEventAsync(stream, new EventAppendEntry("Event", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
            )
        ).ToList();

        var results = await Task.WhenAll(tasks);
        var globalSequences = results.Select(r => r.GlobalSequence).OrderBy(x => x).ToList();

        Assert.Equal(50, globalSequences.Count);
        Assert.Equal(Enumerable.Range(1, 50).Select(x => (long)x), globalSequences);
    }

    [Fact]
    public async Task ReadStreamAsync_WithExactRange_ReturnsExactEvents()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        await store.AppendEventsAsync(streamId, Enumerable.Range(1, 10)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(5, 5));

        Assert.Single(result.Events);
        Assert.Equal(5, result.Events[0].Sequence);
    }

    [Fact]
    public async Task ReadStreamAsync_WithRangeBeyondStream_ReturnsAvailableEvents()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        await store.AppendEventsAsync(streamId, Enumerable.Range(1, 5)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(3, 100));

        Assert.Equal(3, result.Events.Count);
        Assert.Equal(3, result.Events[0].Sequence);
        Assert.Equal(5, result.Events[2].Sequence);
    }

    [Fact]
    public async Task ReadStreamAsync_WithInvertedRange_ReturnsEmpty()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        await store.AppendEventsAsync(streamId, Enumerable.Range(1, 5)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(10, 5));

        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task ReadAllAsync_WithLimit_ReturnsLimitedEvents()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        await store.AppendEventsAsync(streamId, Enumerable.Range(1, 100)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));

        var result = await store.ReadAllAsync(1, 10);

        Assert.Equal(10, result.Events.Count);
        Assert.Equal(100, result.TotalCount);
    }

    [Fact]
    public async Task ReadAllAsync_WithFromSequence_StartsFromCorrectPosition()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        await store.AppendEventsAsync(streamId, Enumerable.Range(1, 10)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));

        var result = await store.ReadAllAsync(5, 100);

        Assert.Equal(6, result.Events.Count);
        Assert.Equal(5, result.Events[0].GlobalSequence);
        Assert.Equal(10, result.Events[5].GlobalSequence);
    }

    [Fact]
    public async Task ReadAllAsync_WithNoEvents_ReturnsEmpty()
    {
        var store = new InMemoryEventStore();

        var result = await store.ReadAllAsync(1, 100);

        Assert.Empty(result.Events);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetStreamLengthAsync_AfterMultipleAppends_ReturnsCorrectLength()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("test-stream");
        var entry = new EventAppendEntry("Event", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow);

        await store.AppendEventAsync(streamId, entry);
        await store.AppendEventAsync(streamId, entry);
        await store.AppendEventAsync(streamId, entry);

        var length = await store.GetStreamLengthAsync(streamId);

        Assert.Equal(3, length);
    }

    [Fact]
    public async Task ConcurrentBatchAppends_ToSameStream_MaintainSequenceIntegrity()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("concurrent-batch-stream");
        
        var tasks = Enumerable.Range(1, 10).Select(batch =>
            Task.Run(async () =>
            {
                var entries = Enumerable.Range(1, 10)
                    .Select(i => new EventAppendEntry($"Batch{batch}_Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
                    .ToList();
                return await store.AppendEventsAsync(streamId, entries);
            })
        ).ToList();

        await Task.WhenAll(tasks);

        var length = await store.GetStreamLengthAsync(streamId);
        Assert.Equal(100, length);

        // Verify all sequences from 1 to 100 exist
        var result = await store.ReadStreamAsync(streamId, new EventReadRange(1, 100));
        var sequences = result.Events.Select(e => e.Sequence).OrderBy(x => x).ToList();
        Assert.Equal(Enumerable.Range(1, 100).Select(x => (long)x), sequences);
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_DoNotInterfere()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("read-write-stream");
        var entry = new EventAppendEntry("Event", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow);

        var writeTasks = Enumerable.Range(1, 50)
            .Select(_ => store.AppendEventAsync(streamId, entry))
            .ToList();

        var readTasks = Enumerable.Range(1, 50)
            .Select(_ => store.ReadStreamAsync(streamId, new EventReadRange(1, 1000)))
            .ToList();

        await Task.WhenAll(writeTasks.Cast<Task>().Concat(readTasks));

        var finalLength = await store.GetStreamLengthAsync(streamId);
        Assert.Equal(50, finalLength);
    }

    [Fact]
    public async Task AppendEventAsync_PreservesEventData()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("data-stream");
        var payload = "test-payload"u8.ToArray();
        var metadata = "test-metadata"u8.ToArray();
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new EventAppendEntry("TestEvent", payload, metadata, timestamp);

        await store.AppendEventAsync(streamId, entry);

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(1, 1));
        var envelope = result.Events[0];

        Assert.Equal("TestEvent", envelope.EventType);
        Assert.Equal(payload, envelope.Payload);
        Assert.Equal(metadata, envelope.Metadata);
        Assert.Equal(timestamp, envelope.TimestampUtc);
    }

    [Fact]
    public async Task ReadAllAsync_AcrossMultipleStreams_MaintainsGlobalOrder()
    {
        var store = new InMemoryEventStore();
        var streams = Enumerable.Range(1, 5)
            .Select(i => new EventStreamId($"stream-{i}"))
            .ToList();

        // Interleave appends across streams
        var tasks = new List<Task<AppendResult>>();
        for (int i = 0; i < 20; i++)
        {
            var stream = streams[i % 5];
            tasks.Add(store.AppendEventAsync(stream, 
                new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow)));
        }

        await Task.WhenAll(tasks);

        var result = await store.ReadAllAsync(1, 100);
        var globalSequences = result.Events.Select(e => e.GlobalSequence).ToList();

        Assert.Equal(Enumerable.Range(1, 20).Select(x => (long)x), globalSequences);
    }

    [Fact]
    public async Task BatchAppend_Verification_AllEventsHaveCorrectStreamId()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("stream-id-check");
        var entries = Enumerable.Range(1, 10)
            .Select(i => new EventAppendEntry($"Event{i}", Array.Empty<byte>(), Array.Empty<byte>(), DateTimeOffset.UtcNow))
            .ToList();

        await store.AppendEventsAsync(streamId, entries);

        var result = await store.ReadStreamAsync(streamId, new EventReadRange(1, 10));

        Assert.All(result.Events, e => Assert.Equal(streamId, e.StreamId));
    }

    [Fact]
    public async Task SaveSnapshotAsync_ThenLoadSnapshotAsync_ReturnsLatestSnapshot()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("snapshot-stream");
        var older = new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2));
        var newer = new EventSnapshot(streamId, 20, "v20"u8.ToArray(), DateTimeOffset.UtcNow);

        await store.SaveSnapshotAsync(older);
        await store.SaveSnapshotAsync(newer);

        var loaded = await store.LoadSnapshotAsync(streamId);

        Assert.NotNull(loaded);
        Assert.Equal(20, loaded.Value.Version);
    }

    [Fact]
    public async Task LoadSnapshotAsync_WithMaxVersion_ReturnsSnapshotAtOrBelowVersion()
    {
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("snapshot-stream-filter");
        await store.SaveSnapshotAsync(new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2)));
        await store.SaveSnapshotAsync(new EventSnapshot(streamId, 25, "v25"u8.ToArray(), DateTimeOffset.UtcNow));

        var loaded = await store.LoadSnapshotAsync(streamId, maxVersion: 20);

        Assert.NotNull(loaded);
        Assert.Equal(10, loaded.Value.Version);
    }
}
