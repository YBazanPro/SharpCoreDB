// <copyright file="EventSnapshot.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Represents a snapshot for a stream to reduce replay cost.
/// </summary>
/// <param name="StreamId">The stream identifier.</param>
/// <param name="Version">The stream version represented by this snapshot.</param>
/// <param name="SnapshotData">Serialized snapshot payload.</param>
/// <param name="CreatedAtUtc">Snapshot creation timestamp in UTC.</param>
public readonly record struct EventSnapshot(
    EventStreamId StreamId,
    long Version,
    ReadOnlyMemory<byte> SnapshotData,
    DateTimeOffset CreatedAtUtc);
