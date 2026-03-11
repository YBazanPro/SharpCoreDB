// <copyright file="DistributedShardOperationsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2025-2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.Distributed.Replication;
using SharpCoreDB.Distributed.Sharding;

namespace SharpCoreDB.Tests;

/// <summary>
/// Tests distributed shard lifecycle operations.
/// </summary>
public sealed class DistributedShardOperationsTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"SharpCoreDB_Distributed_{Guid.NewGuid():N}");

    [Fact]
    public async Task CreateShardAsync_WithPathConnection_InitializesShardMetadata()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var shardManager = new ShardManager();
        var shardOperations = CreateShardOperations(shardManager);
        var shardPath = Path.Combine(_testRoot, "create-shard");

        // Act
        await shardOperations.CreateShardAsync("shard-1", $"Path={shardPath};Password=testpass", isMaster: true, cancellationToken);

        // Assert
        var shard = shardManager.GetShard("shard-1");
        Assert.NotNull(shard);
        Assert.True(shard.IsMaster);
        Assert.Equal(ShardStatus.Online, shard.Status);

        using var serviceProvider = CreateServiceProvider();
        var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
        var db = factory.Create(shardPath, "testpass");

        try
        {
            var rows = db.ExecuteQuery("SELECT * FROM __shard_metadata");
            Assert.Single(rows);
            Assert.Equal("shard-1", rows[0]["ShardId"]);
            Assert.Equal("Master", rows[0]["Role"]);
        }
        finally
        {
            (db as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task MigrateTableAsync_WithSourceRows_CopiesSchemaAndDataToTargetShard()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var shardManager = new ShardManager();
        var shardOperations = CreateShardOperations(shardManager);
        var sourcePath = Path.Combine(_testRoot, "source-shard");
        var targetPath = Path.Combine(_testRoot, "target-shard");

        await shardOperations.CreateShardAsync("source", $"Path={sourcePath};Password=testpass", isMaster: true, cancellationToken);
        await shardOperations.CreateShardAsync("target", $"Path={targetPath};Password=testpass", cancellationToken: cancellationToken);

        using (var serviceProvider = CreateServiceProvider())
        {
            var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
            var sourceDb = factory.Create(sourcePath, "testpass");

            try
            {
                sourceDb.ExecuteSQL("CREATE TABLE customers (id INTEGER PRIMARY KEY, name TEXT)");
                sourceDb.ExecuteSQL("INSERT INTO customers VALUES (1, 'Alice')");
                sourceDb.Flush();
                sourceDb.ForceSave();
            }
            finally
            {
                (sourceDb as IDisposable)?.Dispose();
            }
        }

        // Act
        await shardOperations.MigrateTableAsync("source", "target", "customers", cancellationToken);

        // Assert
        using var targetProvider = CreateServiceProvider();
        var targetFactory = targetProvider.GetRequiredService<DatabaseFactory>();
        var targetDb = targetFactory.Create(targetPath, "testpass");

        try
        {
            var rows = targetDb.ExecuteQuery("SELECT * FROM customers");
            Assert.Single(rows);
            Assert.Equal("Alice", rows[0]["name"]);
        }
        finally
        {
            (targetDb as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task RemoveShardAsync_WithReplicaConfigured_PromotesReplicaToMaster()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var shardManager = new ShardManager();
        var shardOperations = CreateShardOperations(shardManager);
        var masterPath = Path.Combine(_testRoot, "master-shard");
        var replicaPath = Path.Combine(_testRoot, "replica-shard");

        await shardOperations.CreateShardAsync("master", $"Path={masterPath};Password=testpass", isMaster: true, cancellationToken);
        await shardOperations.CreateShardAsync("replica", $"Path={replicaPath};Password=testpass", cancellationToken: cancellationToken);

        await using var replicationManager = new ReplicationManager(shardManager);
        await replicationManager.StartAsync(cancellationToken);
        await replicationManager.AddReplicaAsync("master", "replica", cancellationToken);
        await WaitForReplicaAssignmentAsync(shardManager, "replica", cancellationToken);

        // Act
        await shardOperations.RemoveShardAsync("master", cancellationToken);

        // Assert
        Assert.Null(shardManager.GetShard("master"));
        var promotedReplica = shardManager.GetShard("replica");
        Assert.NotNull(promotedReplica);
        Assert.True(promotedReplica.IsMaster);
        Assert.Null(promotedReplica.MasterShardId);
    }

    [Fact]
    public async Task StartMonitoringAsync_WithHealthyShard_UpdatesLastHealthCheck()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var shardManager = new ShardManager();
        var shardOperations = CreateShardOperations(shardManager);
        var shardPath = Path.Combine(_testRoot, "monitor-shard");
        await shardOperations.CreateShardAsync("monitored", $"Path={shardPath};Password=testpass", isMaster: true, cancellationToken);

        await using var monitor = new ShardMonitor(
            shardManager,
            shardOperations,
            healthCheckInterval: TimeSpan.FromMilliseconds(50));

        // Act
        await monitor.StartMonitoringAsync(cancellationToken);
        var stats = monitor.GetMonitorStats();
        await monitor.StopMonitoringAsync();

        // Assert
        Assert.NotEqual(default, stats.LastHealthCheck);
        Assert.True(stats.IsMonitoring);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Thread.Sleep(100);
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private static ShardOperations CreateShardOperations(ShardManager shardManager)
    {
        return new ShardOperations(shardManager, new ShardRouter(shardManager, new HashShardKey("TenantId")));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        return services.BuildServiceProvider();
    }

    private static async Task WaitForReplicaAssignmentAsync(ShardManager shardManager, string replicaShardId, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < timeoutAt)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(shardManager.GetShard(replicaShardId)?.MasterShardId))
            {
                return;
            }

            await Task.Delay(50, cancellationToken);
        }

        throw new TimeoutException($"Replica shard '{replicaShardId}' was not assigned to a master within the timeout.");
    }
}
