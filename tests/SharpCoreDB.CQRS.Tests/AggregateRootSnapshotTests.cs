// <copyright file="AggregateRootSnapshotTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS.Tests;

using System.Text;
using System.Text.Json;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="AggregateRoot"/> snapshot support.
/// </summary>
public class AggregateRootSnapshotTests
{
    [Fact]
    public void SupportsSnapshots_WithDefaultAggregate_ReturnsFalse()
    {
        // Arrange
        var aggregate = new NonSnapshotAggregate();

        // Assert
        Assert.False(aggregate.SupportsSnapshots);
    }

    [Fact]
    public void SupportsSnapshots_WithSnapshotAggregate_ReturnsTrue()
    {
        // Arrange
        var aggregate = new SnapshotAggregate();

        // Assert
        Assert.True(aggregate.SupportsSnapshots);
    }

    [Fact]
    public void CreateSnapshot_WithDefaultAggregate_ReturnsNull()
    {
        // Arrange
        var aggregate = new NonSnapshotAggregate();

        // Act
        var snapshot = aggregate.CreateSnapshot();

        // Assert
        Assert.Null(snapshot);
    }

    [Fact]
    public void CreateSnapshot_WithSnapshotAggregate_ReturnsPayload()
    {
        // Arrange
        var aggregate = new SnapshotAggregate();
        aggregate.SetBalance(42);

        // Act
        var snapshot = aggregate.CreateSnapshot();

        // Assert
        Assert.NotNull(snapshot);
        var state = JsonSerializer.Deserialize<SnapshotAggregate.State>(snapshot.Value.Span);
        Assert.NotNull(state);
        Assert.Equal(42, state.Balance);
    }

    [Fact]
    public void RestoreFromSnapshot_WithSnapshotAggregate_RestoresState()
    {
        // Arrange
        var aggregate = new SnapshotAggregate();
        aggregate.SetBalance(100);
        var snapshot = aggregate.CreateSnapshot()!.Value;

        var restored = new SnapshotAggregate();

        // Act
        restored.RestoreFromSnapshot(snapshot, version: 5);

        // Assert
        Assert.Equal(100, restored.CurrentBalance);
        Assert.Equal(5, restored.Version);
    }

    [Fact]
    public void RestoreFromSnapshot_ThenRehydrate_AppliesIncrementalEvents()
    {
        // Arrange
        var aggregate = new SnapshotAggregate();
        aggregate.SetBalance(50);
        var snapshot = aggregate.CreateSnapshot()!.Value;

        var restored = new SnapshotAggregate();
        restored.RestoreFromSnapshot(snapshot, version: 3);

        var incrementalEvents = new[]
        {
            new EventEnvelope(new EventStreamId("acc"), 4, 4, "BalanceChanged", Encoding.UTF8.GetBytes("75"), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
            new EventEnvelope(new EventStreamId("acc"), 5, 5, "BalanceChanged", Encoding.UTF8.GetBytes("200"), ReadOnlyMemory<byte>.Empty, DateTimeOffset.UtcNow),
        };

        // Act
        restored.Rehydrate(incrementalEvents);

        // Assert
        Assert.Equal(200, restored.CurrentBalance);
        Assert.Equal(5, restored.Version);
    }

    [Fact]
    public void RestoreFromSnapshot_WithDefaultAggregate_DoesNotThrow()
    {
        // Arrange
        var aggregate = new NonSnapshotAggregate();

        // Act & Assert — no-op should not throw
        aggregate.RestoreFromSnapshot("data"u8.ToArray(), version: 10);
    }

    /// <summary>
    /// Aggregate that does NOT override snapshot methods — default behavior.
    /// </summary>
    private sealed class NonSnapshotAggregate : AggregateRoot
    {
        protected override void ApplyEnvelope(EventEnvelope envelope) { }
    }

    /// <summary>
    /// Aggregate that supports snapshots via CreateSnapshot/RestoreFromSnapshot.
    /// </summary>
    private sealed class SnapshotAggregate : AggregateRoot
    {
        public int CurrentBalance { get; private set; }

        public override bool SupportsSnapshots => true;

        public void SetBalance(int balance) => CurrentBalance = balance;

        public override ReadOnlyMemory<byte>? CreateSnapshot()
        {
            var state = new State { Balance = CurrentBalance };
            return JsonSerializer.SerializeToUtf8Bytes(state);
        }

        public override void RestoreFromSnapshot(ReadOnlyMemory<byte> snapshotData, long version)
        {
            var state = JsonSerializer.Deserialize<State>(snapshotData.Span);
            if (state is not null)
            {
                CurrentBalance = state.Balance;
            }

            Version = version;
        }

        protected override void ApplyEnvelope(EventEnvelope envelope)
        {
            if (envelope.EventType == "BalanceChanged" &&
                int.TryParse(Encoding.UTF8.GetString(envelope.Payload.Span), out var balance))
            {
                CurrentBalance = balance;
            }
        }

        public sealed class State
        {
            public int Balance { get; set; }
        }
    }
}
