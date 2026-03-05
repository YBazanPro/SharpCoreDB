// <copyright file="AppendResult.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Result of an append operation on a stream.
/// </summary>
/// <param name="StreamId">The stream identifier.</param>
/// <param name="AppendedSequence">The assigned sequence number for the appended event.</param>
/// <param name="GlobalSequence">The global sequence number for projection ordering.</param>
/// <param name="Success">Whether the append succeeded.</param>
public readonly record struct AppendResult(
    EventStreamId StreamId,
    long AppendedSequence,
    long GlobalSequence,
    bool Success)
{
    /// <summary>
    /// Creates a failed append result.
    /// </summary>
    public static AppendResult Failure(EventStreamId streamId) => new(streamId, -1, -1, false);

    /// <summary>
    /// Creates a successful append result.
    /// </summary>
    public static AppendResult Ok(EventStreamId streamId, long sequence, long globalSequence) =>
        new(streamId, sequence, globalSequence, true);
}
