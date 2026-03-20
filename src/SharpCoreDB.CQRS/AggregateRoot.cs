// <copyright file="AggregateRoot.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Base aggregate root for CQRS + EventSourcing command handling.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<EventAppendEntry> _pendingEvents = [];

    /// <summary>
    /// Gets current aggregate version.
    /// </summary>
    public long Version { get; protected set; }

    /// <summary>
    /// Gets pending events raised by command execution.
    /// </summary>
    public IReadOnlyList<EventAppendEntry> PendingEvents => _pendingEvents;

    /// <summary>
    /// Appends an event to pending list and increments version.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="payload">Event payload.</param>
    /// <param name="metadata">Event metadata.</param>
    protected void RaiseEvent(string eventType, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        _pendingEvents.Add(new EventAppendEntry(
            eventType,
            payload,
            metadata,
            DateTimeOffset.UtcNow));

        Version++;
    }

    /// <summary>
    /// Clears pending events after persistence.
    /// </summary>
    public void ClearPendingEvents() => _pendingEvents.Clear();

    /// <summary>
    /// Publishes all pending events to the outbox for reliable downstream delivery,
    /// then clears the pending list. This is the ES-Outbox bridge entry point.
    /// </summary>
    /// <param name="aggregateId">Aggregate identifier that raised the events.</param>
    /// <param name="publisher">Outbox event publisher to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of events successfully published to the outbox.</returns>
    public async Task<int> PublishPendingEventsToOutboxAsync(
        string aggregateId,
        IOutboxEventPublisher publisher,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(publisher);

        var published = 0;
        foreach (var entry in _pendingEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await publisher.PublishToOutboxAsync(aggregateId, entry, cancellationToken).ConfigureAwait(false))
            {
                published++;
            }
        }

        _pendingEvents.Clear();
        return published;
    }

    /// <summary>
    /// Rehydrates aggregate from stream envelopes.
    /// </summary>
    /// <param name="events">Stream events in sequence order.</param>
    public void Rehydrate(IReadOnlyList<EventEnvelope> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var envelope in events)
        {
            ApplyEnvelope(envelope);
            Version = envelope.Sequence;
        }
    }

    /// <summary>
    /// Creates a snapshot of the current aggregate state for persistence.
    /// Override in derived aggregates to enable snapshot support.
    /// </summary>
    /// <returns>Serialized snapshot payload, or <see langword="null"/> if snapshots are not supported.</returns>
    public virtual ReadOnlyMemory<byte>? CreateSnapshot() => null;

    /// <summary>
    /// Restores aggregate state from a snapshot payload.
    /// Override in derived aggregates alongside <see cref="CreateSnapshot"/> to enable snapshot support.
    /// </summary>
    /// <param name="snapshotData">Serialized snapshot payload previously returned by <see cref="CreateSnapshot"/>.</param>
    /// <param name="version">The stream version the snapshot was taken at.</param>
    public virtual void RestoreFromSnapshot(ReadOnlyMemory<byte> snapshotData, long version)
    {
        // Default: no-op. Override to restore aggregate state from snapshot.
    }

    /// <summary>
    /// Indicates whether this aggregate supports snapshot creation and restoration.
    /// Override to return <see langword="true"/> in aggregates that implement
    /// <see cref="CreateSnapshot"/> and <see cref="RestoreFromSnapshot"/>.
    /// </summary>
    public virtual bool SupportsSnapshots => false;

    /// <summary>
    /// Applies an event envelope to aggregate state.
    /// </summary>
    /// <param name="envelope">Event envelope.</param>
    protected abstract void ApplyEnvelope(EventEnvelope envelope);
}
