// <copyright file="SharpCoreDbProjectionCheckpointStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;

/// <summary>
/// Integration tests for <see cref="SharpCoreDbProjectionCheckpointStore"/>.
/// </summary>
public class SharpCoreDbProjectionCheckpointStoreTests
{
    [Fact]
    public async Task SaveCheckpointAsync_AfterReopen_LoadsPersistedCheckpoint()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = GetTempDatabasePath();

        var initialStore = CreateStore(databasePath);
        await initialStore.SaveCheckpointAsync(
            new ProjectionCheckpoint("OrdersProjection", "main", "tenant-a", 123, DateTimeOffset.UtcNow),
            cancellationToken);

        var reopenedStore = CreateStore(databasePath);
        var checkpoint = await reopenedStore.GetCheckpointAsync("OrdersProjection", "main", "tenant-a", cancellationToken);

        Assert.Equal(123, checkpoint?.GlobalSequence);
    }

    private static SharpCoreDbProjectionCheckpointStore CreateStore(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
        var database = factory.Create(databasePath, "projection-checkpoint-test-password");

        return new SharpCoreDbProjectionCheckpointStore(database);
    }

    private static string GetTempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"SharpCoreDB_ProjectionCheckpoint_{Guid.NewGuid():N}");
}
