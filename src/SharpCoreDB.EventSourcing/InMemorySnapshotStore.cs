// <copyright file="InMemorySnapshotStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe in-memory <see cref="ISnapshotStore"/> for testing and lightweight scenarios.
/// Stores snapshots per stream sorted by version.
/// </summary>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<string, List<EventSnapshot>> _snapshots = [];

    /// <inheritdoc />
    public Task SaveAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_lock)
        {
            var streamSnapshots = _snapshots.GetOrAdd(snapshot.StreamId.Value, _ => []);
            var existingIndex = streamSnapshots.FindIndex(s => s.Version == snapshot.Version);
            if (existingIndex >= 0)
            {
                streamSnapshots[existingIndex] = snapshot;
            }
            else
            {
                streamSnapshots.Add(snapshot);
            }

            streamSnapshots.Sort(static (left, right) => left.Version.CompareTo(right.Version));
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<EventSnapshot?> LoadLatestAsync(
        EventStreamId streamId,
        long maxVersion = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<EventSnapshot?>(cancellationToken);
        }

        lock (_lock)
        {
            if (!_snapshots.TryGetValue(streamId.Value, out var streamSnapshots) || streamSnapshots.Count == 0)
            {
                return Task.FromResult<EventSnapshot?>(null);
            }

            var snapshot = streamSnapshots
                .Where(s => s.Version <= maxVersion)
                .OrderByDescending(s => s.Version)
                .Cast<EventSnapshot?>()
                .FirstOrDefault();

            return Task.FromResult(snapshot);
        }
    }

    /// <inheritdoc />
    public Task<int> DeleteAllAsync(EventStreamId streamId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        lock (_lock)
        {
            if (!_snapshots.TryRemove(streamId.Value, out var removed))
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(removed.Count);
        }
    }
}
