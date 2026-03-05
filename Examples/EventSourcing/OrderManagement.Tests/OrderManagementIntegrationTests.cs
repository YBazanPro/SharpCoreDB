// <copyright file="OrderManagementIntegrationTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace OrderManagement.Tests;

using SharpCoreDB.EventSourcing;
using Xunit;

/// <summary>
/// Integration tests for Order Management System demo.
/// Validates all 5 demo scenarios work correctly.
/// </summary>
public class OrderManagementIntegrationTests
{
    /// <summary>
    /// Test Scenario 1: Create and evolve order through complete lifecycle.
    /// </summary>
    [Fact]
    public async Task Scenario1_CreateAndEvolveOrder_CompletesSuccessfully()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var orderId = "ORDER-TEST-001";
        var customerId = "CUST-123";

        // Act - Create order
        var order = OrderAggregate.CreateOrder(orderId, customerId, [
            new OrderItem { ProductId = "PROD-1", ProductName = "Laptop", Quantity = 1, Price = 999.99m },
            new OrderItem { ProductId = "PROD-2", ProductName = "Mouse", Quantity = 2, Price = 29.99m }
        ]);

        Assert.Equal(orderId, order.OrderId);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(1059.97m, order.TotalAmount);
        await PersistEvents(eventStore, orderId, order);

        // Act - Add item
        order.AddItem("PROD-3", "Keyboard", 1, 79.99m);
        Assert.Equal(3, order.Items.Count);
        Assert.Equal(1139.96m, order.TotalAmount);
        await PersistEvents(eventStore, orderId, order);

        // Act - Confirm order
        order.ConfirmOrder();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        await PersistEvents(eventStore, orderId, order);

        // Act - Mark as paid
        order.MarkAsPaid(order.TotalAmount, "CreditCard", "TXN-789");
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("TXN-789", order.PaymentTransactionId);
        await PersistEvents(eventStore, orderId, order);

        // Act - Ship order
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(3);
        order.ShipOrder("TRACK-123", "FedEx", estimatedDelivery);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("TRACK-123", order.TrackingNumber);
        await PersistEvents(eventStore, orderId, order);

        // Act - Deliver order
        order.MarkAsDelivered("John Doe");
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.DeliveredAt);
        await PersistEvents(eventStore, orderId, order);

        // Assert - Verify stream has all events
        var streamId = new EventStreamId(orderId);
        var events = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, long.MaxValue));
        Assert.Equal(6, events.Events.Count);
    }

    /// <summary>
    /// Test Scenario 2: Rebuild state from events.
    /// </summary>
    [Fact]
    public async Task Scenario2_RebuildStateFromEvents_ReconstructsCorrectly()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var orderId = "ORDER-TEST-002";
        var customerId = "CUST-456";

        // Create and evolve order
        var order = OrderAggregate.CreateOrder(orderId, customerId, [
            new OrderItem { ProductId = "PROD-1", ProductName = "Monitor", Quantity = 1, Price = 299.99m }
        ]);
        await PersistEvents(eventStore, orderId, order);

        order.ConfirmOrder();
        await PersistEvents(eventStore, orderId, order);

        order.MarkAsPaid(299.99m, "PayPal", "TXN-999");
        await PersistEvents(eventStore, orderId, order);

        // Act - Rebuild from events
        var streamId = new EventStreamId(orderId);
        var events = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, long.MaxValue));
        var rebuiltOrder = OrderAggregate.FromEventStream(events.Events);

        // Assert
        Assert.Equal(orderId, rebuiltOrder.OrderId);
        Assert.Equal(customerId, rebuiltOrder.CustomerId);
        Assert.Equal(OrderStatus.Paid, rebuiltOrder.Status);
        Assert.Single(rebuiltOrder.Items);
        Assert.Equal(299.99m, rebuiltOrder.TotalAmount);
        Assert.Equal("TXN-999", rebuiltOrder.PaymentTransactionId);
        Assert.Equal(3, rebuiltOrder.Version);
    }

    /// <summary>
    /// Test Scenario 3: Multiple orders and global feed.
    /// </summary>
    [Fact]
    public async Task Scenario3_MultipleOrdersAndGlobalFeed_WorksCorrectly()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();

        // Act - Create order 1
        var order1 = OrderAggregate.CreateOrder("ORDER-001", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item1", Quantity = 1, Price = 100m }
        ]);
        order1.ConfirmOrder();
        await PersistEvents(eventStore, "ORDER-001", order1);

        // Act - Create order 2
        var order2 = OrderAggregate.CreateOrder("ORDER-002", "CUST-2", [
            new OrderItem { ProductId = "PROD-2", ProductName = "Item2", Quantity = 2, Price = 50m }
        ]);
        order2.ConfirmOrder();
        order2.MarkAsPaid(100m, "Card", "TXN-1");
        await PersistEvents(eventStore, "ORDER-002", order2);

        // Act - Create order 3
        var order3 = OrderAggregate.CreateOrder("ORDER-003", "CUST-3", [
            new OrderItem { ProductId = "PROD-3", ProductName = "Item3", Quantity = 1, Price = 75m }
        ]);
        order3.CancelOrder("Customer changed mind", "System");
        await PersistEvents(eventStore, "ORDER-003", order3);

        // Act - Read global feed
        var globalFeed = await eventStore.ReadAllAsync(1, 100);

        // Assert
        Assert.Equal(7, globalFeed.TotalCount); // 2 + 3 + 2 events
        Assert.Equal(7, globalFeed.Events.Count);

        // Verify global sequences are contiguous
        var globalSequences = globalFeed.Events.Select(e => e.GlobalSequence).ToList();
        Assert.Equal(Enumerable.Range(1, 7).Select(x => (long)x), globalSequences);

        // Verify streams are separate
        var streamGroups = globalFeed.Events.GroupBy(e => e.StreamId.Value).ToList();
        Assert.Equal(3, streamGroups.Count);
    }

    /// <summary>
    /// Test Scenario 4: Point-in-time query.
    /// </summary>
    [Fact]
    public async Task Scenario4_PointInTimeQuery_ReturnsCorrectState()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var orderId = "ORDER-PIT-001";

        var order = OrderAggregate.CreateOrder(orderId, "CUST-999", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Product", Quantity = 1, Price = 100m }
        ]);
        await PersistEvents(eventStore, orderId, order);

        order.ConfirmOrder();
        await PersistEvents(eventStore, orderId, order);

        order.MarkAsPaid(100m, "Cash", "TXN-PIT");
        await PersistEvents(eventStore, orderId, order);

        order.ShipOrder("TRACK-PIT", "UPS", DateTimeOffset.UtcNow.AddDays(2));
        await PersistEvents(eventStore, orderId, order);

        // Act - Query at sequence 2 (after confirmation, before payment)
        var streamId = new EventStreamId(orderId);
        var partialEvents = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, 2));
        var orderAtPoint = OrderAggregate.FromEventStream(partialEvents.Events);

        // Assert
        Assert.Equal(OrderStatus.Confirmed, orderAtPoint.Status);
        Assert.Equal(2, orderAtPoint.Version);
        Assert.Null(orderAtPoint.PaymentTransactionId); // Not yet paid
        Assert.Null(orderAtPoint.TrackingNumber); // Not yet shipped

        // Act - Query current state (all events)
        var allEvents = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, long.MaxValue));
        var currentOrder = OrderAggregate.FromEventStream(allEvents.Events);

        // Assert
        Assert.Equal(OrderStatus.Shipped, currentOrder.Status);
        Assert.Equal(4, currentOrder.Version);
        Assert.Equal("TXN-PIT", currentOrder.PaymentTransactionId);
        Assert.Equal("TRACK-PIT", currentOrder.TrackingNumber);
    }

    /// <summary>
    /// Test Scenario 5: Stream statistics.
    /// </summary>
    [Fact]
    public async Task Scenario5_StreamStatistics_ReturnsCorrectMetadata()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();

        // Create orders with different event counts
        var order1 = OrderAggregate.CreateOrder("ORDER-STAT-001", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item1", Quantity = 1, Price = 100m }
        ]);
        order1.ConfirmOrder();
        order1.MarkAsPaid(100m, "Card", "TXN-1");
        await PersistEvents(eventStore, "ORDER-STAT-001", order1);

        var order2 = OrderAggregate.CreateOrder("ORDER-STAT-002", "CUST-2", [
            new OrderItem { ProductId = "PROD-2", ProductName = "Item2", Quantity = 1, Price = 200m }
        ]);
        await PersistEvents(eventStore, "ORDER-STAT-002", order2);

        // Act - Get stream lengths
        var length1 = await eventStore.GetStreamLengthAsync(new EventStreamId("ORDER-STAT-001"));
        var length2 = await eventStore.GetStreamLengthAsync(new EventStreamId("ORDER-STAT-002"));
        var lengthNonExistent = await eventStore.GetStreamLengthAsync(new EventStreamId("ORDER-STAT-999"));

        // Assert
        Assert.Equal(3, length1); // Created + Confirmed + Paid
        Assert.Equal(1, length2); // Only Created
        Assert.Equal(0, lengthNonExistent); // Doesn't exist
    }

    /// <summary>
    /// Test: Business rule validation - cannot add item to confirmed order.
    /// </summary>
    [Fact]
    public async Task BusinessRule_CannotAddItemToConfirmedOrder_ThrowsException()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var order = OrderAggregate.CreateOrder("ORDER-BR-001", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item", Quantity = 1, Price = 100m }
        ]);
        await PersistEvents(eventStore, "ORDER-BR-001", order);

        order.ConfirmOrder();
        await PersistEvents(eventStore, "ORDER-BR-001", order);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.AddItem("PROD-2", "Item2", 1, 50m)
        );

        Assert.Contains("Cannot add items", exception.Message);
    }

    /// <summary>
    /// Test: Business rule validation - cannot confirm empty order.
    /// </summary>
    [Fact]
    public void BusinessRule_CannotConfirmEmptyOrder_ThrowsException()
    {
        // Arrange
        var order = OrderAggregate.CreateOrder("ORDER-BR-002", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item", Quantity = 1, Price = 100m }
        ]);

        // Remove all items
        order.RemoveItem("PROD-1", 1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.ConfirmOrder()
        );

        Assert.Contains("Cannot confirm order with no items", exception.Message);
    }

    /// <summary>
    /// Test: Business rule validation - cannot cancel delivered order.
    /// </summary>
    [Fact]
    public async Task BusinessRule_CannotCancelDeliveredOrder_ThrowsException()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var order = OrderAggregate.CreateOrder("ORDER-BR-003", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item", Quantity = 1, Price = 100m }
        ]);
        await PersistEvents(eventStore, "ORDER-BR-003", order);

        order.ConfirmOrder();
        await PersistEvents(eventStore, "ORDER-BR-003", order);

        order.MarkAsPaid(100m, "Card", "TXN-1");
        await PersistEvents(eventStore, "ORDER-BR-003", order);

        order.ShipOrder("TRACK-1", "FedEx", DateTimeOffset.UtcNow.AddDays(2));
        await PersistEvents(eventStore, "ORDER-BR-003", order);

        order.MarkAsDelivered("John Doe");
        await PersistEvents(eventStore, "ORDER-BR-003", order);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.CancelOrder("Changed mind", "Customer")
        );

        Assert.Contains("Cannot cancel delivered order", exception.Message);
    }

    /// <summary>
    /// Test: Event serialization and deserialization works correctly.
    /// </summary>
    [Fact]
    public void EventSerialization_RoundTrip_PreservesData()
    {
        // Arrange
        var originalEvent = new OrderCreatedEvent
        {
            OrderId = "ORDER-SER-001",
            CustomerId = "CUST-123",
            Items = [
                new OrderItem { ProductId = "PROD-1", ProductName = "Laptop", Quantity = 1, Price = 999.99m },
                new OrderItem { ProductId = "PROD-2", ProductName = "Mouse", Quantity = 2, Price = 29.99m }
            ],
            TotalAmount = 1059.97m,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var serialized = originalEvent.Serialize();
        var deserialized = OrderEvent.Deserialize<OrderCreatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.OrderId, deserialized.OrderId);
        Assert.Equal(originalEvent.CustomerId, deserialized.CustomerId);
        Assert.Equal(originalEvent.Items.Count, deserialized.Items.Count);
        Assert.Equal(originalEvent.TotalAmount, deserialized.TotalAmount);
        Assert.Equal(originalEvent.Items[0].ProductId, deserialized.Items[0].ProductId);
        Assert.Equal(originalEvent.Items[0].Quantity, deserialized.Items[0].Quantity);
    }

    /// <summary>
    /// Test: Item addition and removal updates total correctly.
    /// </summary>
    [Fact]
    public void ItemManagement_AddAndRemove_UpdatesTotalCorrectly()
    {
        // Arrange
        var order = OrderAggregate.CreateOrder("ORDER-ITEM-001", "CUST-1", [
            new OrderItem { ProductId = "PROD-1", ProductName = "Item1", Quantity = 2, Price = 50m }
        ]);

        Assert.Equal(100m, order.TotalAmount);

        // Act - Add item
        order.AddItem("PROD-2", "Item2", 1, 30m);

        // Assert
        Assert.Equal(130m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);

        // Act - Remove some quantity
        order.RemoveItem("PROD-1", 1);

        // Assert
        Assert.Equal(80m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(1, order.Items.First(i => i.ProductId == "PROD-1").Quantity);

        // Act - Remove all of an item
        order.RemoveItem("PROD-1", 1);

        // Assert
        Assert.Equal(30m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    // Helper method to persist events
    private static async Task PersistEvents(IEventStore store, string orderId, OrderAggregate order)
    {
        var streamId = new EventStreamId(orderId);

        foreach (var orderEvent in order.PendingEvents)
        {
            var entry = new EventAppendEntry(
                EventType: orderEvent.GetType().Name,
                Payload: orderEvent.Serialize(),
                Metadata: Array.Empty<byte>(),
                TimestampUtc: orderEvent.Timestamp
            );

            await store.AppendEventAsync(streamId, entry);
        }

        order.ClearPendingEvents();
    }
}
