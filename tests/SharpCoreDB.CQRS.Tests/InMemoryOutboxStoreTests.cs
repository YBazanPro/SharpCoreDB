// <copyright file="InMemoryOutboxStoreTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryOutboxStore"/>.
/// </summary>
public class InMemoryOutboxStoreTests
{
    [Fact]
    public async Task AddAsync_ThenGetUnpublishedAsync_ReturnsMessage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();

        await store.AddAsync(
            new OutboxMessage(
                MessageId: "msg-1",
                AggregateId: "order-1",
                MessageType: "OrderPlaced",
                Payload: "{}"u8.ToArray(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IsPublished: false),
            cancellationToken);

        var messages = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Single(messages);
        Assert.Equal("msg-1", messages[0].MessageId);
    }

    [Fact]
    public async Task MarkPublishedAsync_WithExistingMessage_MarksMessageAsPublished()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();

        await store.AddAsync(
            new OutboxMessage(
                MessageId: "msg-2",
                AggregateId: "order-2",
                MessageType: "OrderPaid",
                Payload: "{}"u8.ToArray(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IsPublished: false),
            cancellationToken);

        await store.MarkPublishedAsync("msg-2", cancellationToken);
        var messages = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task RecordFailureAsync_WithExistingMessage_ExcludesMessageUntilRetryWindow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();

        await store.AddAsync(
            new OutboxMessage(
                MessageId: "msg-fail",
                AggregateId: "order-3",
                MessageType: "OrderFailed",
                Payload: "{}"u8.ToArray(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IsPublished: false),
            cancellationToken);

        await store.RecordFailureAsync("msg-fail", "Publisher failure", cancellationToken);
        var messages = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task RecordFailureAsync_WhenMaxAttemptsReached_MovesMessageToDeadLetter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore(new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
        });

        await store.AddAsync(
            new OutboxMessage(
                MessageId: "msg-dead",
                AggregateId: "order-4",
                MessageType: "OrderFailed",
                Payload: "{}"u8.ToArray(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IsPublished: false),
            cancellationToken);

        await store.RecordFailureAsync("msg-dead", "Fatal error", cancellationToken);
        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);
        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Empty(unpublished);
        Assert.Single(deadLetters);
        Assert.Equal("msg-dead", deadLetters[0].MessageId);
    }

    [Fact]
    public async Task RequeueDeadLetterAsync_WithDeadLetteredMessage_MovesMessageBackToUnpublished()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore(new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
        });

        await store.AddAsync(
            new OutboxMessage(
                MessageId: "msg-requeue",
                AggregateId: "order-5",
                MessageType: "OrderFailed",
                Payload: "{}"u8.ToArray(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IsPublished: false),
            cancellationToken);

        await store.RecordFailureAsync("msg-requeue", "Fatal error", cancellationToken);
        await store.RequeueDeadLetterAsync("msg-requeue", cancellationToken);

        var unpublished = await store.GetUnpublishedAsync(10, cancellationToken);
        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Single(unpublished);
        Assert.Equal("msg-requeue", unpublished[0].MessageId);
        Assert.Empty(deadLetters);
    }

    [Fact]
    public async Task RequeueDeadLetterAsync_WithNonExistentMessage_DoesNotThrow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();

        var exception = await Record.ExceptionAsync(
            () => store.RequeueDeadLetterAsync("does-not-exist", cancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateMessageId_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();

        var message = new OutboxMessage(
            MessageId: "duplicate-id",
            AggregateId: "order-1",
            MessageType: "OrderPlaced",
            Payload: "{}"u8.ToArray(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            IsPublished: false);

        var firstAdd = await store.AddAsync(message, cancellationToken);
        var secondAdd = await store.AddAsync(message, cancellationToken);

        Assert.True(firstAdd);
        Assert.False(secondAdd);
    }

    [Fact]
    public async Task AddAsync_WithDeadLetteredMessageId_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore(new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
        });

        var message = new OutboxMessage(
            MessageId: "dead-lettered-id",
            AggregateId: "order-2",
            MessageType: "OrderFailed",
            Payload: "{}"u8.ToArray(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            IsPublished: false);

        await store.AddAsync(message, cancellationToken);
        await store.RecordFailureAsync("dead-lettered-id", "Fatal error", cancellationToken);
        var reAdd = await store.AddAsync(message, cancellationToken);

        Assert.False(reAdd);
    }
}
