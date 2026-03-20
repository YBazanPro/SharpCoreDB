// <copyright file="EventStoreSnapshotPolicyExtensions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Optional helper APIs for snapshot policy-based persistence.
/// </summary>
public static class EventStoreSnapshotPolicyExtensions
{
    /// <summary>
    /// Applies a snapshot policy after appending events to a stream.
    /// </summary>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="policy">Snapshot policy.</param>
    /// <param name="snapshotFactory">Factory used to build snapshot payload when policy matches.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a snapshot was persisted; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> ApplySnapshotPolicyAsync(
        this IEventStore eventStore,
        EventStreamId streamId,
        SnapshotPolicy policy,
        Func<long, EventSnapshot> snapshotFactory,
        CancellationToken cancellationToken = default)
    {
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
        await eventStore.SaveSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Appends events and applies snapshot policy in one call.
    /// </summary>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="entries">Entries to append.</param>
    /// <param name="policy">Snapshot policy.</param>
    /// <param name="snapshotFactory">Factory used to build snapshot payload when policy matches.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The append results and snapshot outcome.</returns>
    public static async Task<AppendWithSnapshotResult> AppendEventsWithSnapshotPolicyAsync(
        this IEventStore eventStore,
        EventStreamId streamId,
        IEnumerable<EventAppendEntry> entries,
        SnapshotPolicy policy,
        Func<long, EventSnapshot> snapshotFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(entries);

        var appendResults = await eventStore.AppendEventsAsync(streamId, entries, cancellationToken).ConfigureAwait(false);
        var snapshotCreated = await eventStore.ApplySnapshotPolicyAsync(streamId, policy, snapshotFactory, cancellationToken).ConfigureAwait(false);
        return new AppendWithSnapshotResult(appendResults, snapshotCreated);
    }
}

/// <summary>
/// Result for append operations that optionally persisted a snapshot.
/// </summary>
/// <param name="AppendResults">Append results for the operation.</param>
/// <param name="SnapshotCreated">Indicates whether a snapshot was persisted.</param>
public readonly record struct AppendWithSnapshotResult(
    IReadOnlyList<AppendResult> AppendResults,
    bool SnapshotCreated);
