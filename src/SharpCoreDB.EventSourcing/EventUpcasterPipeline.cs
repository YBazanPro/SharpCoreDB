// <copyright file="EventUpcasterPipeline.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Ordered event upcasting pipeline for schema evolution.
/// </summary>
public sealed class EventUpcasterPipeline
{
    private readonly IReadOnlyList<IEventUpcaster> _upcasters;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventUpcasterPipeline"/> class.
    /// </summary>
    /// <param name="upcasters">Ordered upcasters to apply.</param>
    public EventUpcasterPipeline(IEnumerable<IEventUpcaster> upcasters)
    {
        ArgumentNullException.ThrowIfNull(upcasters);
        _upcasters = [.. upcasters];
    }

    /// <summary>
    /// Applies all matching upcasters in registration order.
    /// </summary>
    /// <param name="envelope">Event envelope to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed envelope.</returns>
    public async ValueTask<EventEnvelope> UpcastAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        var context = new EventUpcastContext(envelope.StreamId, envelope.Sequence, envelope.GlobalSequence);
        var transformed = envelope;

        foreach (var upcaster in _upcasters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (upcaster.CanUpcast(transformed))
            {
                transformed = await upcaster.UpcastAsync(transformed, context, cancellationToken).ConfigureAwait(false);
            }
        }

        return transformed;
    }

    /// <summary>
    /// Applies upcasters to an event batch in read order.
    /// </summary>
    /// <param name="events">Source events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed event list.</returns>
    public async Task<IReadOnlyList<EventEnvelope>> UpcastManyAsync(
        IReadOnlyList<EventEnvelope> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return [];
        }

        var transformed = new EventEnvelope[events.Count];
        for (var index = 0; index < events.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            transformed[index] = await UpcastAsync(events[index], cancellationToken).ConfigureAwait(false);
        }

        return transformed;
    }
}
