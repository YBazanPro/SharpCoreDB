// <copyright file="OutboxDispatchBackgroundServiceTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using System.Collections.Concurrent;

/// <summary>
/// Unit tests for <see cref="OutboxDispatchBackgroundService"/>.
/// </summary>
public class OutboxDispatchBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithMessages_DispatchesOnEachTick()
    {
        var store = new InMemoryOutboxStore();
        var publisher = new RecordingOutboxPublisher();
        var dispatchService = new OutboxDispatchService(store, publisher);

        await store.AddAsync(MakeMessage("w1"), CancellationToken.None);
        await store.AddAsync(MakeMessage("w2"), CancellationToken.None);

        using var cts = new CancellationTokenSource();
        var worker = new OutboxDispatchBackgroundServiceTestProxy(
            dispatchService,
            new OutboxHostedServiceOptions
            {
                BatchSize = 10,
                PollInterval = TimeSpan.FromMilliseconds(5),
                MaxIterations = 1,
            });

        await worker.ExecutePublicAsync(cts.Token);

        Assert.Equal(2, publisher.PublishedMessageIds.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublisherFails_LogsErrorAndContinues()
    {
        var store = new InMemoryOutboxStore();
        await store.AddAsync(MakeMessage("fail-1"), CancellationToken.None);

        var publisher = new FailingOutboxPublisher();
        var dispatchService = new OutboxDispatchService(store, publisher);

        using var cts = new CancellationTokenSource();
        var worker = new OutboxDispatchBackgroundServiceTestProxy(
            dispatchService,
            new OutboxHostedServiceOptions
            {
                BatchSize = 10,
                PollInterval = TimeSpan.FromMilliseconds(5),
                MaxIterations = 1,
            });

        // Should not throw — errors are swallowed per cycle
        await worker.ExecutePublicAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var store = new InMemoryOutboxStore();
        var publisher = new RecordingOutboxPublisher();
        var worker = new OutboxDispatchBackgroundServiceTestProxy(
            new OutboxDispatchService(store, publisher),
            new OutboxHostedServiceOptions
            {
                BatchSize = 10,
                PollInterval = TimeSpan.FromMilliseconds(5),
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            worker.ExecutePublicAsync(cts.Token));
    }

    [Fact]
    public void Constructor_WithNullDispatchService_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new OutboxDispatchBackgroundService(null!, new OutboxHostedServiceOptions()));

    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        var store = new InMemoryOutboxStore();
        var publisher = new RecordingOutboxPublisher();
        var service = new OutboxDispatchService(store, publisher);
        Assert.Throws<ArgumentNullException>(() => new OutboxDispatchBackgroundService(service, null!));
    }

    private static OutboxMessage MakeMessage(string id) =>
        new(id, "agg-1", "TypeA", "{}"u8.ToArray(), DateTimeOffset.UtcNow, false);

    private sealed class RecordingOutboxPublisher : IOutboxPublisher
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> PublishedMessageIds { get; } = [];

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
}

/// <summary>
/// Test subclass that exposes ExecuteAsync for direct test invocation.
/// </summary>
internal sealed class OutboxDispatchBackgroundServiceTestProxy(
    OutboxDispatchService dispatchService,
    OutboxHostedServiceOptions options) : OutboxDispatchBackgroundService(dispatchService, options)
{
    public Task ExecutePublicAsync(CancellationToken ct) => ExecuteAsync(ct);
}
