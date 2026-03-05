// <copyright file="EventEnvelope.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Represents a persisted event record with stream and sequence metadata.
/// </summary>
/// <param name="StreamId">The logical stream identifier.</param>
/// <param name="Sequence">The per-stream sequence number.</param>
/// <param name="GlobalSequence">The global sequence number for projection-friendly ordering.</param>
/// <param name="EventType">The event type discriminator.</param>
/// <param name="Payload">Serialized event payload.</param>
/// <param name="Metadata">Serialized event metadata.</param>
/// <param name="TimestampUtc">The event timestamp in UTC.</param>
public readonly record struct EventEnvelope(
    EventStreamId StreamId,
    long Sequence,
    long GlobalSequence,
    string EventType,
    ReadOnlyMemory<byte> Payload,
    ReadOnlyMemory<byte> Metadata,
    DateTimeOffset TimestampUtc);
