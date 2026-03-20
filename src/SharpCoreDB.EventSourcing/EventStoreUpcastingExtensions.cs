// <copyright file="EventStoreUpcastingExtensions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Event store read extensions that apply upcasting pipeline transformations.
/// </summary>
public static class EventStoreUpcastingExtensions
{
    /// <summary>
    /// Reads stream events and applies configured upcasters to each envelope.
    /// </summary>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="range">Event range to read.</param>
    /// <param name="upcasters">Ordered upcasters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read result with transformed envelopes and original total count.</returns>
    public static async Task<ReadResult> ReadStreamUpcastedAsync(
        this IEventStore eventStore,
        EventStreamId streamId,
        EventReadRange range,
        IEnumerable<IEventUpcaster> upcasters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(upcasters);

        var read = await eventStore.ReadStreamAsync(streamId, range, cancellationToken).ConfigureAwait(false);
        var pipeline = new EventUpcasterPipeline(upcasters);
        var transformed = await pipeline.UpcastManyAsync(read.Events, cancellationToken).ConfigureAwait(false);
        return ReadResult.Ok(transformed, read.TotalCount);
    }

    /// <summary>
    /// Reads global events and applies configured upcasters to each envelope.
    /// </summary>
    /// <param name="eventStore">Event store instance.</param>
    /// <param name="fromGlobalSequence">Starting global sequence.</param>
    /// <param name="limit">Maximum events to return.</param>
    /// <param name="upcasters">Ordered upcasters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read result with transformed envelopes and original total count.</returns>
    public static async Task<ReadResult> ReadAllUpcastedAsync(
        this IEventStore eventStore,
        long fromGlobalSequence,
        int limit,
        IEnumerable<IEventUpcaster> upcasters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(upcasters);

        var read = await eventStore.ReadAllAsync(fromGlobalSequence, limit, cancellationToken).ConfigureAwait(false);
        var pipeline = new EventUpcasterPipeline(upcasters);
        var transformed = await pipeline.UpcastManyAsync(read.Events, cancellationToken).ConfigureAwait(false);
        return ReadResult.Ok(transformed, read.TotalCount);
    }
}
