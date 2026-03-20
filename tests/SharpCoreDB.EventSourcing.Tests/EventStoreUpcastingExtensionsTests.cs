// <copyright file="EventStoreUpcastingExtensionsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

using System.Text;

/// <summary>
/// Unit tests for upcasting read extension methods.
/// </summary>
public class EventStoreUpcastingExtensionsTests
{
    [Fact]
    public async Task ReadStreamUpcastedAsync_WithMatchingEvents_ReturnsTransformedEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryEventStore();
        var streamId = new EventStreamId("upcast-stream");

        await store.AppendEventAsync(
            streamId,
            new EventAppendEntry("LegacyEvent", Encoding.UTF8.GetBytes("7"), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
            cancellationToken);

        var read = await store.ReadStreamUpcastedAsync(
            streamId,
            new EventReadRange(1, 100),
            [new LegacyEventUpcaster()],
            cancellationToken);

        Assert.Single(read.Events);
        Assert.Equal("ModernEvent", read.Events[0].EventType);
        Assert.Equal("{\"value\":7}", Encoding.UTF8.GetString(read.Events[0].Payload.Span));
    }

    [Fact]
    public async Task ReadAllUpcastedAsync_WithLimit_PreservesTotalCountAndTransformsEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new InMemoryEventStore();

        await store.AppendEventAsync(
            new EventStreamId("s1"),
            new EventAppendEntry("LegacyEvent", Encoding.UTF8.GetBytes("1"), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
            cancellationToken);

        await store.AppendEventAsync(
            new EventStreamId("s2"),
            new EventAppendEntry("LegacyEvent", Encoding.UTF8.GetBytes("2"), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
            cancellationToken);

        var read = await store.ReadAllUpcastedAsync(
            fromGlobalSequence: 1,
            limit: 1,
            upcasters: [new LegacyEventUpcaster()],
            cancellationToken);

        Assert.Equal(1, read.Events.Count);
        Assert.Equal(2, read.TotalCount);
        Assert.Equal("ModernEvent", read.Events[0].EventType);
    }

    private sealed class LegacyEventUpcaster : IEventUpcaster
    {
        public bool CanUpcast(EventEnvelope envelope) => envelope.EventType == "LegacyEvent";

        public ValueTask<EventEnvelope> UpcastAsync(
            EventEnvelope envelope,
            EventUpcastContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = int.Parse(Encoding.UTF8.GetString(envelope.Payload.Span));
            return ValueTask.FromResult(envelope with
            {
                EventType = "ModernEvent",
                Payload = Encoding.UTF8.GetBytes($"{{\"value\":{value}}}"),
            });
        }
    }
}
