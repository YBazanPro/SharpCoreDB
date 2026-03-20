// <copyright file="EventStoreSnapshotLoadExtensions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Snapshot-aware aggregate load helpers for <see cref="IEventStore"/>.
/// </summary>
public static class EventStoreSnapshotLoadExtensions
{
    /// <summary>
    /// Loads an aggregate from the latest snapshot (when available) and replays remaining events.
    /// </summary>
    /// <typeparam name="TAggregate">Aggregate type.</typeparam>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="fromEvents">Factory to create aggregate from a full event list when no snapshot is used.</param>
    /// <param name="fromSnapshot">Factory to create aggregate from a persisted snapshot payload.</param>
    /// <param name="replayFromSnapshot">Replayer to apply events that occur after the snapshot version.</param>
    /// <param name="version">Optional stream version upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Load result containing aggregate, loaded version, replay count, and snapshot metadata.</returns>
    public static async Task<SnapshotLoadResult<TAggregate>> LoadWithSnapshotAsync<TAggregate>(
        this IEventStore eventStore,
        EventStreamId streamId,
        Func<IReadOnlyList<EventEnvelope>, TAggregate> fromEvents,
        Func<ReadOnlyMemory<byte>, TAggregate> fromSnapshot,
        Func<TAggregate, IReadOnlyList<EventEnvelope>, TAggregate> replayFromSnapshot,
        long? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(fromEvents);
        ArgumentNullException.ThrowIfNull(fromSnapshot);
        ArgumentNullException.ThrowIfNull(replayFromSnapshot);

        var maxVersion = version ?? long.MaxValue;
        var snapshot = await eventStore.LoadSnapshotAsync(streamId, maxVersion, cancellationToken).ConfigureAwait(false);

        if (snapshot is not { } loadedSnapshot)
        {
            var fullRead = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, maxVersion), cancellationToken).ConfigureAwait(false);
            var aggregateWithoutSnapshot = fromEvents(fullRead.Events);
            return new SnapshotLoadResult<TAggregate>(aggregateWithoutSnapshot, fullRead.Events.Count, null, fullRead.Events.Count);
        }

        var aggregateFromSnapshot = fromSnapshot(loadedSnapshot.SnapshotData);
        var fromSequence = loadedSnapshot.Version + 1;

        if (fromSequence > maxVersion)
        {
            return new SnapshotLoadResult<TAggregate>(aggregateFromSnapshot, loadedSnapshot.Version, loadedSnapshot, 0);
        }

        var incrementalRead = await eventStore.ReadStreamAsync(streamId, new EventReadRange(fromSequence, maxVersion), cancellationToken).ConfigureAwait(false);
        var aggregateWithReplay = replayFromSnapshot(aggregateFromSnapshot, incrementalRead.Events);
        var loadedVersion = incrementalRead.Events.Count == 0
            ? loadedSnapshot.Version
            : incrementalRead.Events[^1].Sequence;

        return new SnapshotLoadResult<TAggregate>(aggregateWithReplay, loadedVersion, loadedSnapshot, incrementalRead.Events.Count);
    }

    /// <summary>
    /// Loads only the aggregate instance from the latest snapshot and remaining events.
    /// </summary>
    /// <typeparam name="TAggregate">Aggregate type.</typeparam>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="fromEvents">Factory to create aggregate from a full event list when no snapshot is used.</param>
    /// <param name="fromSnapshot">Factory to create aggregate from a persisted snapshot payload.</param>
    /// <param name="replayFromSnapshot">Replayer to apply events that occur after the snapshot version.</param>
    /// <param name="version">Optional stream version upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded aggregate instance.</returns>
    public static async Task<TAggregate> LoadAggregateWithSnapshotAsync<TAggregate>(
        this IEventStore eventStore,
        EventStreamId streamId,
        Func<IReadOnlyList<EventEnvelope>, TAggregate> fromEvents,
        Func<ReadOnlyMemory<byte>, TAggregate> fromSnapshot,
        Func<TAggregate, IReadOnlyList<EventEnvelope>, TAggregate> replayFromSnapshot,
        long? version = null,
        CancellationToken cancellationToken = default)
    {
        var result = await eventStore.LoadWithSnapshotAsync(
            streamId,
            fromEvents,
            fromSnapshot,
            replayFromSnapshot,
            version,
            cancellationToken).ConfigureAwait(false);

        return result.Aggregate;
    }
}

/// <summary>
/// Result of snapshot-aware aggregate loading.
/// </summary>
/// <typeparam name="TAggregate">Aggregate type.</typeparam>
/// <param name="Aggregate">Loaded aggregate instance.</param>
/// <param name="Version">Final stream version represented by <paramref name="Aggregate"/>.</param>
/// <param name="Snapshot">Snapshot used during load, if any.</param>
/// <param name="ReplayedEvents">Number of events replayed after snapshot load.</param>
public readonly record struct SnapshotLoadResult<TAggregate>(
    TAggregate Aggregate,
    long Version,
    EventSnapshot? Snapshot,
    int ReplayedEvents);
