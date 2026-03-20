// <copyright file="AggregateRootTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="AggregateRoot"/>.
/// </summary>
public class AggregateRootTests
{
    [Fact]
    public void RaiseEvent_WithValidData_AddsPendingEventAndIncrementsVersion()
    {
        var aggregate = new TestAggregate();

        aggregate.Create("aggregate-1");

        Assert.Single(aggregate.PendingEvents);
        Assert.Equal(1, aggregate.Version);
    }

    [Fact]
    public void Rehydrate_WithEnvelopeSequence_UpdatesVersion()
    {
        var aggregate = new TestAggregate();
        var events = new[]
        {
            new EventEnvelope(new EventStreamId("agg"), 1, 1, "Created", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
            new EventEnvelope(new EventStreamId("agg"), 2, 2, "Updated", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
        };

        aggregate.Rehydrate(events);

        Assert.Equal(2, aggregate.Version);
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void Create(string id)
        {
            RaiseEvent("Created", "{}"u8.ToArray(), ReadOnlyMemory<byte>.Empty);
        }

        protected override void ApplyEnvelope(EventEnvelope envelope)
        {
        }
    }
}
