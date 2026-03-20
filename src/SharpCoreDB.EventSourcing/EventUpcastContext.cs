// <copyright file="EventUpcastContext.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Context for event upcasting operations.
/// </summary>
/// <param name="StreamId">Event stream identifier.</param>
/// <param name="Sequence">Per-stream sequence number.</param>
/// <param name="GlobalSequence">Global event sequence number.</param>
public readonly record struct EventUpcastContext(
    EventStreamId StreamId,
    long Sequence,
    long GlobalSequence);
