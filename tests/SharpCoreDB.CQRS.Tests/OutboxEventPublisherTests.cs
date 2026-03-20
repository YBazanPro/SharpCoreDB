// <copyright file="OutboxEventPublisherTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="OutboxEventPublisher"/> and the
/// <see cref="AggregateRoot.PublishPendingEventsToOutboxAsync"/> bridge.
/// </summary>
public class OutboxEventPublisherTests
{
    [Fact]
    public async Task PublishToOutboxAsync_WithValidEntry_AddsMessageToStore()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var entry = new EventAppendEntry("OrderPlaced", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow);

        var result = await publisher.PublishToOutboxAsync("order-1", entry, cancellationToken);

        Assert.True(result);
        var messages = await store.GetUnpublishedAsync(10, cancellationToken);
        Assert.Single(messages);
        Assert.Equal("order-1", messages[0].AggregateId);
        Assert.Equal("OrderPlaced", messages[0].MessageType);
    }

    [Fact]
    public async Task PublishToOutboxAsync_WithDuplicateEntry_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new EventAppendEntry("OrderPlaced", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, timestamp);

        await publisher.PublishToOutboxAsync("order-1", entry, cancellationToken);
        var duplicate = await publisher.PublishToOutboxAsync("order-1", entry, cancellationToken);

        Assert.False(duplicate);
    }

    [Fact]
    public async Task PublishToOutboxAsync_WithNullAggregateId_Throws()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var entry = new EventAppendEntry("OrderPlaced", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishToOutboxAsync(null!, entry, cancellationToken));
    }

    [Fact]
    public void Constructor_WithNullStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new OutboxEventPublisher(null!));
    }

    [Fact]
    public async Task AggregateRoot_PublishPendingEventsToOutboxAsync_PublishesAllEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var aggregate = new TestAggregate();

        aggregate.DoWork();
        aggregate.DoWork();

        var published = await aggregate.PublishPendingEventsToOutboxAsync("agg-1", publisher, cancellationToken);

        Assert.Equal(2, published);
        Assert.Empty(aggregate.PendingEvents);
        var messages = await store.GetUnpublishedAsync(10, cancellationToken);
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task AggregateRoot_PublishPendingEventsToOutboxAsync_WithNoEvents_ReturnsZero()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var aggregate = new TestAggregate();

        var published = await aggregate.PublishPendingEventsToOutboxAsync("agg-1", publisher, cancellationToken);

        Assert.Equal(0, published);
        Assert.Empty(aggregate.PendingEvents);
    }

    [Fact]
    public async Task AggregateRoot_PublishPendingEventsToOutboxAsync_WithNullPublisher_Throws()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var aggregate = new TestAggregate();
        aggregate.DoWork();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => aggregate.PublishPendingEventsToOutboxAsync("agg-1", null!, cancellationToken));
    }

    [Fact]
    public async Task AggregateRoot_PublishPendingEventsToOutboxAsync_ClearsPendingAfterPublish()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryOutboxStore();
        var publisher = new OutboxEventPublisher(store);
        var aggregate = new TestAggregate();

        aggregate.DoWork();
        await aggregate.PublishPendingEventsToOutboxAsync("agg-1", publisher, cancellationToken);

        Assert.Empty(aggregate.PendingEvents);
        Assert.Equal(1, aggregate.Version);
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void DoWork()
        {
            RaiseEvent("WorkDone", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty);
        }

        protected override void ApplyEnvelope(EventEnvelope envelope)
        {
        }
    }
}
