// <copyright file="OutboxDispatchServiceTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using System.Collections.Concurrent;

/// <summary>
/// Unit tests for <see cref="OutboxDispatchService"/>.
/// </summary>
public class OutboxDispatchServiceTests
{
    [Fact]
    public async Task DispatchUnpublishedAsync_WithMessages_PublishesAndMarksPublished()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new RecordingOutboxPublisher();
        var service = new OutboxDispatchService(store, publisher);

        await store.AddAsync(new OutboxMessage("m1", "a1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);
        await store.AddAsync(new OutboxMessage("m2", "a1", "TypeB", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);

        var published = await service.DispatchUnpublishedAsync(10, cancellationToken);
        var remaining = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Equal(2, published);
        Assert.Equal(2, publisher.PublishedMessageIds.Count);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DispatchUnpublishedAsync_WhenPublisherThrows_RecordsFailureAndDoesNotThrow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new FailingOutboxPublisher();
        var service = new OutboxDispatchService(store, publisher);

        await store.AddAsync(new OutboxMessage("m-fail", "a1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);

        var published = await service.DispatchUnpublishedAsync(10, cancellationToken);
        var remaining = await store.GetUnpublishedAsync(10, cancellationToken);

        Assert.Equal(0, published);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DispatchUnpublishedAsync_WhenOneMessageFails_ContinuesProcessingOthers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new SelectiveFailingOutboxPublisher("m-fail");
        var service = new OutboxDispatchService(store, publisher);

        await store.AddAsync(new OutboxMessage("m-fail", "a1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);
        await store.AddAsync(new OutboxMessage("m-ok", "a1", "TypeB", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);

        var published = await service.DispatchUnpublishedAsync(10, cancellationToken);

        Assert.Equal(1, published);
        Assert.Contains("m-ok", publisher.PublishedMessageIds);
    }

    [Fact]
    public async Task DispatchUnpublishedAsync_WhenPublisherThrowsAndMaxAttemptsReached_MovesToDeadLetter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore(new OutboxRetryPolicyOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
        });
        var publisher = new FailingOutboxPublisher();
        var service = new OutboxDispatchService(store, publisher);

        await store.AddAsync(new OutboxMessage("m-dead", "a1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false), cancellationToken);

        var published = await service.DispatchUnpublishedAsync(10, cancellationToken);
        var deadLetters = await store.GetDeadLettersAsync(10, cancellationToken);

        Assert.Equal(0, published);
        Assert.Single(deadLetters);
        Assert.Equal("m-dead", deadLetters[0].MessageId);
    }

    private sealed class RecordingOutboxPublisher : IOutboxPublisher
    {
        public ConcurrentQueue<string> PublishedMessageIds { get; } = [];

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishedMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingOutboxPublisher : IOutboxPublisher
    {
        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Publisher failure");
        }
    }

    private sealed class SelectiveFailingOutboxPublisher(string failingMessageId) : IOutboxPublisher
    {
        public ConcurrentQueue<string> PublishedMessageIds { get; } = [];

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(message.MessageId, failingMessageId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Publisher failure");
            }

            PublishedMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }
    }
}
