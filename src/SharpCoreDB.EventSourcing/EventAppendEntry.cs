// <copyright file="EventAppendEntry.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Represents a single append request entry for an event stream.
/// </summary>
/// <param name="EventType">The event type discriminator.</param>
/// <param name="Payload">Serialized event payload.</param>
/// <param name="Metadata">Serialized event metadata.</param>
/// <param name="TimestampUtc">The event timestamp in UTC.</param>
public readonly record struct EventAppendEntry(
    string EventType,
    ReadOnlyMemory<byte> Payload,
    ReadOnlyMemory<byte> Metadata,
    DateTimeOffset TimestampUtc);
