// <copyright file="InMemorySnapshotStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

public sealed class InMemorySnapshotStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadLatestAsync_ReturnsSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-123");
        var snapshot = new EventSnapshot(streamId, 5, "state-v5"u8.ToArray(), DateTimeOffset.UtcNow);

        // Act
        await store.SaveAsync(snapshot);
        var loaded = await store.LoadLatestAsync(streamId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(5, loaded.Value.Version);
        Assert.Equal("state-v5"u8.ToArray(), loaded.Value.SnapshotData.ToArray());
    }

    [Fact]
    public async Task LoadLatestAsync_WithNoSnapshots_ReturnsNull()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("nonexistent");

        // Act
        var loaded = await store.LoadLatestAsync(streamId);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadLatestAsync_WithMultipleSnapshots_ReturnsHighestVersion()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-456");
        await store.SaveAsync(new EventSnapshot(streamId, 5, "v5"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2)));
        await store.SaveAsync(new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-1)));
        await store.SaveAsync(new EventSnapshot(streamId, 20, "v20"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var loaded = await store.LoadLatestAsync(streamId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(20, loaded.Value.Version);
        Assert.Equal("v20"u8.ToArray(), loaded.Value.SnapshotData.ToArray());
    }

    [Fact]
    public async Task LoadLatestAsync_WithMaxVersion_ReturnsSnapshotAtOrBelowVersion()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-789");
        await store.SaveAsync(new EventSnapshot(streamId, 5, "v5"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2)));
        await store.SaveAsync(new EventSnapshot(streamId, 15, "v15"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-1)));
        await store.SaveAsync(new EventSnapshot(streamId, 25, "v25"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var loaded = await store.LoadLatestAsync(streamId, maxVersion: 20);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(15, loaded.Value.Version);
    }

    [Fact]
    public async Task LoadLatestAsync_WithMaxVersionBelowAllSnapshots_ReturnsNull()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-low");
        await store.SaveAsync(new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var loaded = await store.LoadLatestAsync(streamId, maxVersion: 5);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveAsync_SameVersion_ReplacesExisting()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-replace");
        await store.SaveAsync(new EventSnapshot(streamId, 10, "old"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-1)));

        // Act
        await store.SaveAsync(new EventSnapshot(streamId, 10, "new"u8.ToArray(), DateTimeOffset.UtcNow));
        var loaded = await store.LoadLatestAsync(streamId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("new"u8.ToArray(), loaded.Value.SnapshotData.ToArray());
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllSnapshotsForStream()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-del");
        await store.SaveAsync(new EventSnapshot(streamId, 5, "v5"u8.ToArray(), DateTimeOffset.UtcNow));
        await store.SaveAsync(new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var deleted = await store.DeleteAllAsync(streamId);

        // Assert
        Assert.Equal(2, deleted);
        Assert.Null(await store.LoadLatestAsync(streamId));
    }

    [Fact]
    public async Task DeleteAllAsync_WithNoSnapshots_ReturnsZero()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamId = new EventStreamId("order-empty");

        // Act
        var deleted = await store.DeleteAllAsync(streamId);

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task DeleteAllAsync_DoesNotAffectOtherStreams()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var streamA = new EventStreamId("stream-a");
        var streamB = new EventStreamId("stream-b");
        await store.SaveAsync(new EventSnapshot(streamA, 5, "a-v5"u8.ToArray(), DateTimeOffset.UtcNow));
        await store.SaveAsync(new EventSnapshot(streamB, 5, "b-v5"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        await store.DeleteAllAsync(streamA);

        // Assert
        Assert.Null(await store.LoadLatestAsync(streamA));
        Assert.NotNull(await store.LoadLatestAsync(streamB));
    }
}
