// <copyright file="IOutboxStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Outbox storage contract for reliable message publishing.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Adds a message to outbox.
    /// </summary>
    /// <param name="message">Outbox message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if added, false if duplicate message ID.</returns>
    Task<bool> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads unpublished outbox messages.
    /// </summary>
    /// <param name="limit">Maximum messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unpublished outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as published.
    /// </summary>
    /// <param name="messageId">Message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkPublishedAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a publish failure and schedules the next retry attempt.
    /// </summary>
    /// <param name="messageId">Message identifier.</param>
    /// <param name="error">Failure details for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordFailureAsync(string messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads dead-lettered outbox messages.
    /// </summary>
    /// <param name="limit">Maximum messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dead-lettered outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a dead-lettered message back to the outbox and resets its retry state,
    /// allowing it to be picked up and published on the next dispatch cycle.
    /// </summary>
    /// <param name="messageId">Message identifier of the dead-lettered message to requeue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RequeueDeadLetterAsync(string messageId, CancellationToken cancellationToken = default);
}
