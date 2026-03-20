// <copyright file="IOutboxPublisher.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Publishes outbox messages to external channels.
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes an outbox message.
    /// </summary>
    /// <param name="message">Outbox message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
