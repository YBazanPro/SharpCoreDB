// <copyright file="SnapshotStoreLoadExtensions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Snapshot-aware aggregate load helpers that use the standalone <see cref="ISnapshotStore"/>
/// for snapshot I/O and <see cref="IEventStore"/> for event replay. This decouples snapshot
/// storage from event storage.
/// </summary>
public static class SnapshotStoreLoadExtensions
{
    /// <summary>
    /// Loads an aggregate from the latest snapshot in <paramref name="snapshotStore"/> (when available)
    /// and replays remaining events from <paramref name="eventStore"/>.
    /// </summary>
    /// <typeparam name="TAggregate">Aggregate type.</typeparam>
    /// <param name="snapshotStore">Standalone snapshot store.</param>
    /// <param name="eventStore">Event store for reading event streams.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="fromEvents">Factory to create aggregate from a full event list when no snapshot exists.</param>
    /// <param name="fromSnapshot">Factory to create aggregate from a persisted snapshot payload.</param>
    /// <param name="replayFromSnapshot">Replayer to apply events that occur after the snapshot version.</param>
    /// <param name="version">Optional stream version upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Load result containing aggregate, loaded version, replay count, and snapshot metadata.</returns>
    public static async Task<SnapshotLoadResult<TAggregate>> LoadWithSnapshotAsync<TAggregate>(
        this ISnapshotStore snapshotStore,
        IEventStore eventStore,
        EventStreamId streamId,
        Func<IReadOnlyList<EventEnvelope>, TAggregate> fromEvents,
        Func<ReadOnlyMemory<byte>, TAggregate> fromSnapshot,
        Func<TAggregate, IReadOnlyList<EventEnvelope>, TAggregate> replayFromSnapshot,
        long? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(fromEvents);
        ArgumentNullException.ThrowIfNull(fromSnapshot);
        ArgumentNullException.ThrowIfNull(replayFromSnapshot);

        var maxVersion = version ?? long.MaxValue;
        var snapshot = await snapshotStore.LoadLatestAsync(streamId, maxVersion, cancellationToken).ConfigureAwait(false);

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
    /// Applies a snapshot policy using the standalone <see cref="ISnapshotStore"/>.
    /// Checks stream length from <paramref name="eventStore"/> and saves the snapshot to <paramref name="snapshotStore"/>.
    /// </summary>
    /// <param name="snapshotStore">Standalone snapshot store.</param>
    /// <param name="eventStore">Event store for reading stream length.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="policy">Snapshot policy.</param>
    /// <param name="snapshotFactory">Factory used to build snapshot payload when policy matches.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a snapshot was persisted; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> ApplySnapshotPolicyAsync(
        this ISnapshotStore snapshotStore,
        IEventStore eventStore,
        EventStreamId streamId,
        SnapshotPolicy policy,
        Func<long, EventSnapshot> snapshotFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(snapshotFactory);

        if (policy.EveryNEvents <= 0)
        {
            return false;
        }

        var streamLength = await eventStore.GetStreamLengthAsync(streamId, cancellationToken).ConfigureAwait(false);
        if (!policy.ShouldCreateSnapshot(streamLength))
        {
            return false;
        }

        var snapshot = snapshotFactory(streamLength);
        await snapshotStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
