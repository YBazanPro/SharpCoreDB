// <copyright file="SharpCoreDbOutboxStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;

/// <summary>
/// Integration tests for <see cref="SharpCoreDbOutboxStore"/>.
/// </summary>
public class SharpCoreDbOutboxStoreTests
{
    [Fact]
    public async Task AddAsync_GetUnpublishedAsync_ReturnsPersistedMessage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());
        var message = MakeMessage("m1");

        await store.AddAsync(message, cancellationToken);
        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("m1", unpublished[0].MessageId);
        Assert.Equal("TypeA", unpublished[0].MessageType);
        Assert.False(unpublished[0].IsPublished);
    }

    [Fact]
    public async Task AddAsync_AfterReopen_MessageStillUnpublished()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store1 = CreateStore(path);

        await store1.AddAsync(MakeMessage("persist-1"), cancellationToken);

        var store2 = CreateStore(path);
        var unpublished = await store2.GetUnpublishedAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("persist-1", unpublished[0].MessageId);
    }

    [Fact]
    public async Task MarkPublishedAsync_ExcludesMessageFromUnpublishedResults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());

        await store.AddAsync(MakeMessage("m-pub"), cancellationToken);
        await store.AddAsync(MakeMessage("m-unpub"), cancellationToken);
        await store.MarkPublishedAsync("m-pub", cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("m-unpub", unpublished[0].MessageId);
    }

    [Fact]
    public async Task GetUnpublishedAsync_RespectsLimit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());

        await store.AddAsync(MakeMessage("a1"), cancellationToken);
        await store.AddAsync(MakeMessage("a2"), cancellationToken);
        await store.AddAsync(MakeMessage("a3"), cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(2, cancellationToken);

        Assert.Equal(2, unpublished.Count);
    }

    [Fact]
    public async Task GetUnpublishedAsync_WithZeroLimit_ReturnsEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());
        await store.AddAsync(MakeMessage("z1"), cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(0, cancellationToken);

        Assert.Empty(unpublished);
    }

    [Fact]
    public void Constructor_WithInvalidTableName_Throws()
    {
        var database = CreateDatabase(GetTempDatabasePath());
        Assert.Throws<ArgumentException>(() => new SharpCoreDbOutboxStore(database, "bad table name!"));
    }

    [Fact]
    public void Constructor_WithNullDatabase_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SharpCoreDbOutboxStore(null!));
    }

    [Fact]
    public async Task GetUnpublishedAsync_ExcludesMessagesScheduledForFuture()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store = CreateStore(path);

        await store.AddAsync(MakeMessage("due"), cancellationToken);
        await store.AddAsync(MakeMessage("future"), cancellationToken);

        var database = CreateDatabase(path);
        Assert.True(database.TryGetTable("scdb_outbox", out var table));

        table.Update(
            "message_id = 'future'",
            new Dictionary<string, object>
            {
                ["next_attempt_utc"] = DateTimeOffset.UtcNow.AddMinutes(10).ToString("O", CultureInfo.InvariantCulture),
            });
        database.Flush();
        database.ForceSave();

        var storeAfterUpdate = CreateStore(path);
        var unpublished = await storeAfterUpdate.GetUnpublishedAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("due", unpublished[0].MessageId);
    }

    [Fact]
    public async Task AddAsync_WithLegacySchema_PersistsMessageWithoutSchemaUpgrade()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var database = CreateDatabase(path);

        database.ExecuteSQL("CREATE TABLE scdb_outbox (message_id TEXT PRIMARY KEY, aggregate_id TEXT, message_type TEXT, payload_hex TEXT, created_at_utc TEXT)");
        database.ExecuteBatchSQL([
            $"INSERT INTO scdb_outbox VALUES ('legacy-1', 'agg-legacy', 'TypeA', '7B7D', '{DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)}')"
        ]);
        database.Flush();
        database.ForceSave();

        var store = new SharpCoreDbOutboxStore(database);
        await store.AddAsync(MakeMessage("new-1"), cancellationToken);
        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Equal(2, unpublished.Count);
    }

    [Fact]
    public async Task RecordFailureAsync_WithExistingMessage_UpdatesRetryMetadataAndHidesMessageUntilDue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store = CreateStore(path);

        await store.AddAsync(MakeMessage("m-failure"), cancellationToken);
        await store.RecordFailureAsync("m-failure", "Publisher failure", cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);
        var database = CreateDatabase(path);
        var rows = database.ExecuteQuery(
            "SELECT attempt_count, last_error, next_attempt_utc FROM scdb_outbox WHERE message_id = 'm-failure'",
            new Dictionary<string, object?>());

        Assert.Empty(unpublished);
        Assert.Single(rows);
        Assert.Equal("1", rows[0]["attempt_count"]?.ToString());
        Assert.Equal("Publisher failure", rows[0]["last_error"]?.ToString());
    }

    [Fact]
    public async Task RecordFailureAsync_WhenMaxAttemptsReached_MovesMessageToDeadLetter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store = CreateStore(path, new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            DeadLetterTableName = "scdb_outbox_deadletter",
        });

        await store.AddAsync(MakeMessage("m-dead"), cancellationToken);
        await store.RecordFailureAsync("m-dead", "Fatal publisher failure", cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);
        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Empty(unpublished);
        Assert.Single(deadLetters);
        Assert.Equal("m-dead", deadLetters[0].MessageId);
    }

    [Fact]
    public async Task GetDeadLettersAsync_WithNoDeadLetters_ReturnsEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());

        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Empty(deadLetters);
    }

    [Fact]
    public async Task RequeueDeadLetterAsync_MovesMessageFromDeadLetterToOutbox()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store = CreateStore(path, new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            DeadLetterTableName = "scdb_outbox_deadletter",
        });

        await store.AddAsync(MakeMessage("m-requeue"), cancellationToken);
        await store.RecordFailureAsync("m-requeue", "Fatal failure", cancellationToken);
        await store.RequeueDeadLetterAsync("m-requeue", cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);
        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("m-requeue", unpublished[0].MessageId);
        Assert.Empty(deadLetters);
    }

    [Fact]
    public async Task RequeueDeadLetterAsync_ResetsAttemptCountInOutboxTable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = GetTempDatabasePath();
        var store = CreateStore(path, new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            DeadLetterTableName = "scdb_outbox_deadletter",
        });

        await store.AddAsync(MakeMessage("m-reset"), cancellationToken);
        await store.RecordFailureAsync("m-reset", "Fatal failure", cancellationToken);
        await store.RequeueDeadLetterAsync("m-reset", cancellationToken);

        var database = CreateDatabase(path);
        var rows = database.ExecuteQuery(
            "SELECT attempt_count FROM scdb_outbox WHERE message_id = 'm-reset'",
            new Dictionary<string, object?>());

        Assert.Single(rows);
        Assert.Equal("0", rows[0]["attempt_count"]?.ToString());
    }

    [Fact]
    public async Task RequeueDeadLetterAsync_WithNonExistentMessage_DoesNotThrow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());

        var exception = await Record.ExceptionAsync(
            () => store.RequeueDeadLetterAsync("does-not-exist", cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateMessageId_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath());
        var message = MakeMessage("dup-1");

        var firstAdd = await store.AddAsync(message, cancellationToken);
        var secondAdd = await store.AddAsync(message, cancellationToken);

        Assert.True(firstAdd);
        Assert.False(secondAdd);
    }

    [Fact]
    public async Task AddAsync_WithDeadLetteredMessageId_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = CreateStore(GetTempDatabasePath(), new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            DeadLetterTableName = "scdb_outbox_deadletter",
        });

        var message = MakeMessage("dead-dup-1");
        await store.AddAsync(message, cancellationToken);
        await store.RecordFailureAsync("dead-dup-1", "Fatal error", cancellationToken);

        var reAdd = await store.AddAsync(message, cancellationToken);

        Assert.False(reAdd);
    }

    private static SharpCoreDbOutboxStore CreateStore(string databasePath)
    {
        var database = CreateDatabase(databasePath);
        return new SharpCoreDbOutboxStore(database);
    }

    private static SharpCoreDbOutboxStore CreateStore(string databasePath, OutboxRetryPolicyOptions retryPolicy)
    {
        var database = CreateDatabase(databasePath);
        return new SharpCoreDbOutboxStore(database, retryPolicy: retryPolicy);
    }

    private static SharpCoreDB.Interfaces.IDatabase CreateDatabase(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
        return factory.Create(databasePath, "outbox-test-password");
    }

    private static OutboxMessage MakeMessage(string id) =>
        new(id, "agg-1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false);

    private static string GetTempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"SharpCoreDB_Outbox_{Guid.NewGuid():N}");
}
