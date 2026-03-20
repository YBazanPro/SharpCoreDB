// <copyright file="ISnapshotStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Standalone snapshot storage contract, decoupled from <see cref="IEventStore"/>.
/// Allows snapshot persistence to be configured and replaced independently of the event store,
/// enabling scenarios like separate snapshot databases, caching layers, or custom retention policies.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves or replaces a snapshot for a specific stream version.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the latest snapshot for a stream up to the specified version.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="maxVersion">The maximum stream version to consider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest matching snapshot, or <see langword="null"/> when no snapshot exists.</returns>
    Task<EventSnapshot?> LoadLatestAsync(
        EventStreamId streamId,
        long maxVersion = long.MaxValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all snapshots for a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of snapshots deleted.</returns>
    Task<int> DeleteAllAsync(EventStreamId streamId, CancellationToken cancellationToken = default);
}
