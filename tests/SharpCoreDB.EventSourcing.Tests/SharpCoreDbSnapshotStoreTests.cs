// <copyright file="SharpCoreDbSnapshotStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;

/// <summary>
/// Integration tests for <see cref="SharpCoreDbSnapshotStore"/> persistence behavior.
/// </summary>
public class SharpCoreDbSnapshotStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadLatestAsync_ReturnsSnapshot()
    {
        // Arrange
        var store = CreateStore();
        var streamId = new EventStreamId("order-100");
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
        var store = CreateStore();
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
        var store = CreateStore();
        var streamId = new EventStreamId("order-multi");
        await store.SaveAsync(new EventSnapshot(streamId, 5, "v5"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2)));
        await store.SaveAsync(new EventSnapshot(streamId, 15, "v15"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-1)));
        await store.SaveAsync(new EventSnapshot(streamId, 25, "v25"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var loaded = await store.LoadLatestAsync(streamId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(25, loaded.Value.Version);
    }

    [Fact]
    public async Task LoadLatestAsync_WithMaxVersion_ReturnsSnapshotAtOrBelowVersion()
    {
        // Arrange
        var store = CreateStore();
        var streamId = new EventStreamId("order-filter");
        await store.SaveAsync(new EventSnapshot(streamId, 10, "v10"u8.ToArray(), DateTimeOffset.UtcNow.AddMinutes(-2)));
        await store.SaveAsync(new EventSnapshot(streamId, 30, "v30"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act
        var loaded = await store.LoadLatestAsync(streamId, maxVersion: 20);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(10, loaded.Value.Version);
    }

    [Fact]
    public async Task SaveAsync_SameVersion_ReplacesExisting()
    {
        // Arrange
        var store = CreateStore();
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
        var store = CreateStore();
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
        var store = CreateStore();

        // Act
        var deleted = await store.DeleteAllAsync(new EventStreamId("empty-stream"));

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task SaveAsync_ThenReopen_PreservesData()
    {
        // Arrange
        var databasePath = GetTempDatabasePath();
        var streamId = new EventStreamId("order-persist");
        var firstStore = CreateStore(databasePath);
        await firstStore.SaveAsync(new EventSnapshot(streamId, 7, "v7"u8.ToArray(), DateTimeOffset.UtcNow));

        // Act — reopen from same path
        var secondStore = CreateStore(databasePath);
        var loaded = await secondStore.LoadLatestAsync(streamId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(7, loaded.Value.Version);
    }

    private static SharpCoreDbSnapshotStore CreateStore(string? databasePath = null)
    {
        databasePath ??= GetTempDatabasePath();
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
        var database = factory.Create(databasePath, "snapshot-store-test-password");

        return new SharpCoreDbSnapshotStore(database);
    }

    private static string GetTempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"SharpCoreDB_SnapshotStore_{Guid.NewGuid():N}");
}
