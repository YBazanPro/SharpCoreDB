// <copyright file="OutboxEventPublisher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using Microsoft.Extensions.Logging;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Default <see cref="IOutboxEventPublisher"/> that converts <see cref="EventAppendEntry"/>
/// instances into <see cref="OutboxMessage"/> records and stores them in the outbox.
/// </summary>
public sealed class OutboxEventPublisher(
    IOutboxStore outboxStore,
    ILogger<OutboxEventPublisher>? logger = null) : IOutboxEventPublisher
{
    private readonly IOutboxStore _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
    private readonly ILogger<OutboxEventPublisher>? _logger = logger;

    /// <inheritdoc />
    public async Task<bool> PublishToOutboxAsync(
        string aggregateId,
        EventAppendEntry entry,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.EventType);

        var message = new OutboxMessage(
            MessageId: $"{aggregateId}-{entry.EventType}-{entry.TimestampUtc.Ticks}",
            AggregateId: aggregateId,
            MessageType: entry.EventType,
            Payload: entry.Payload,
            CreatedAtUtc: entry.TimestampUtc,
            IsPublished: false);

        var added = await _outboxStore.AddAsync(message, cancellationToken).ConfigureAwait(false);

        if (added)
        {
            _logger?.LogDebug("Event {EventType} for aggregate {AggregateId} published to outbox as {MessageId}.",
                entry.EventType, aggregateId, message.MessageId);
        }
        else
        {
            _logger?.LogWarning("Duplicate outbox message {MessageId} for aggregate {AggregateId} was rejected.",
                message.MessageId, aggregateId);
        }

        return added;
    }
}
