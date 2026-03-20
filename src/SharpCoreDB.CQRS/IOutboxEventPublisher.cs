// <copyright file="IOutboxEventPublisher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Bridge that converts domain events raised by an <see cref="AggregateRoot"/>
/// into <see cref="OutboxMessage"/> entries for reliable downstream delivery.
/// </summary>
public interface IOutboxEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the outbox store.
    /// </summary>
    /// <param name="aggregateId">Aggregate identifier that raised the event.</param>
    /// <param name="entry">The event append entry to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was added to the outbox; false if a duplicate message ID was detected.</returns>
    Task<bool> PublishToOutboxAsync(string aggregateId, EventAppendEntry entry, CancellationToken cancellationToken = default);
}
