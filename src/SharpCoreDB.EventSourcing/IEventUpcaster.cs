// <copyright file="IEventUpcaster.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Defines a transformation from one persisted event schema to a newer schema.
/// </summary>
public interface IEventUpcaster
{
    /// <summary>
    /// Determines whether this upcaster should transform the provided event envelope.
    /// </summary>
    /// <param name="envelope">Event envelope candidate.</param>
    /// <returns><see langword="true"/> when this upcaster can transform the event.</returns>
    bool CanUpcast(EventEnvelope envelope);

    /// <summary>
    /// Transforms an event envelope to the target schema.
    /// </summary>
    /// <param name="envelope">Event envelope to transform.</param>
    /// <param name="context">Upcast execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transformed event envelope.</returns>
    ValueTask<EventEnvelope> UpcastAsync(
        EventEnvelope envelope,
        EventUpcastContext context,
        CancellationToken cancellationToken = default);
}
