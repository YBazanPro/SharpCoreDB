// <copyright file="ReadResult.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Result of a read operation on events.
/// </summary>
/// <param name="Events">The read event envelopes.</param>
/// <param name="TotalCount">Total count of events matching the read criteria.</param>
public readonly record struct ReadResult(
    IReadOnlyList<EventEnvelope> Events,
    long TotalCount)
{
    /// <summary>
    /// Creates an empty read result.
    /// </summary>
    public static ReadResult Empty() => new([], 0);

    /// <summary>
    /// Creates a read result with events.
    /// </summary>
    public static ReadResult Ok(IReadOnlyList<EventEnvelope> events, long totalCount) =>
        new(events, totalCount);
}
