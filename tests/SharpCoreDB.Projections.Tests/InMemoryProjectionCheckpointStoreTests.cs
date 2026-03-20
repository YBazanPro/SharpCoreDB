// <copyright file="InMemoryProjectionCheckpointStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryProjectionCheckpointStore"/>.
/// </summary>
public class InMemoryProjectionCheckpointStoreTests
{
    [Fact]
    public async Task SaveCheckpointAsync_WithCheckpoint_SavesAndLoadsCheckpoint()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryProjectionCheckpointStore();
        var checkpoint = new ProjectionCheckpoint("OrdersProjection", "main", "tenant-a", 42, DateTimeOffset.UtcNow);

        await store.SaveCheckpointAsync(checkpoint, cancellationToken);

        var loaded = await store.GetCheckpointAsync("OrdersProjection", "main", "tenant-a", cancellationToken);
        Assert.Equal(42, loaded?.GlobalSequence);
    }
}
