// <copyright file="OutboxMessage.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Outbox message used for reliable downstream delivery.
/// </summary>
/// <param name="MessageId">Message identifier.</param>
/// <param name="AggregateId">Aggregate identifier.</param>
/// <param name="MessageType">Message type discriminator.</param>
/// <param name="Payload">Serialized message payload.</param>
/// <param name="CreatedAtUtc">Message creation timestamp.</param>
/// <param name="IsPublished">Publish state.</param>
public readonly record struct OutboxMessage(
    string MessageId,
    string AggregateId,
    string MessageType,
    ReadOnlyMemory<byte> Payload,
    DateTimeOffset CreatedAtUtc,
    bool IsPublished);
