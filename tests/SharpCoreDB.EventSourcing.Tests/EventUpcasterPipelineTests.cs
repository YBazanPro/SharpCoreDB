// <copyright file="EventUpcasterPipelineTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

using System.Text;

/// <summary>
/// Unit tests for event upcasting pipeline behavior.
/// </summary>
public class EventUpcasterPipelineTests
{
    [Fact]
    public async Task UpcastAsync_WithSingleMatchingUpcaster_TransformsEventTypeAndPayload()
    {
        var pipeline = new EventUpcasterPipeline([new ValueAddedV1ToV2Upcaster()]);
        var envelope = CreateEnvelope("ValueAddedV1", "10");

        var transformed = await pipeline.UpcastAsync(envelope, TestContext.Current.CancellationToken);

        Assert.Equal("ValueAddedV2", transformed.EventType);
        Assert.Equal("{\"value\":10}", Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    [Fact]
    public async Task UpcastAsync_WithChainedUpcasters_AppliesAllMatchingTransformationsInOrder()
    {
        var pipeline = new EventUpcasterPipeline(
        [
            new ValueAddedV1ToV2Upcaster(),
            new ValueAddedV2ToV3Upcaster(),
        ]);

        var envelope = CreateEnvelope("ValueAddedV1", "12");
        var transformed = await pipeline.UpcastAsync(envelope, TestContext.Current.CancellationToken);

        Assert.Equal("ValueAddedV3", transformed.EventType);
        Assert.Equal("{\"amount\":12,\"currency\":\"USD\"}", Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    private static EventEnvelope CreateEnvelope(string eventType, string payload) =>
        new(
            new EventStreamId("stream-1"),
            Sequence: 1,
            GlobalSequence: 1,
            EventType: eventType,
            Payload: Encoding.UTF8.GetBytes(payload),
            Metadata: ReadOnlyMemory<byte>.Empty,
            TimestampUtc: DateTimeOffset.UtcNow);

    private sealed class ValueAddedV1ToV2Upcaster : IEventUpcaster
    {
        public bool CanUpcast(EventEnvelope envelope) => envelope.EventType == "ValueAddedV1";

        public ValueTask<EventEnvelope> UpcastAsync(
            EventEnvelope envelope,
            EventUpcastContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = int.Parse(Encoding.UTF8.GetString(envelope.Payload.Span));
            var transformed = envelope with
            {
                EventType = "ValueAddedV2",
                Payload = Encoding.UTF8.GetBytes($"{{\"value\":{value}}}"),
            };

            return ValueTask.FromResult(transformed);
        }
    }

    private sealed class ValueAddedV2ToV3Upcaster : IEventUpcaster
    {
        public bool CanUpcast(EventEnvelope envelope) => envelope.EventType == "ValueAddedV2";

        public ValueTask<EventEnvelope> UpcastAsync(
            EventEnvelope envelope,
            EventUpcastContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = Encoding.UTF8.GetString(envelope.Payload.Span);
            var value = payload.Split(':')[1].TrimEnd('}');
            var transformed = envelope with
            {
                EventType = "ValueAddedV3",
                Payload = Encoding.UTF8.GetBytes($"{{\"amount\":{value},\"currency\":\"USD\"}}"),
            };

            return ValueTask.FromResult(transformed);
        }
    }
}
