// <copyright file="Program.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using OrderManagement;
using SharpCoreDB.EventSourcing;

Console.WriteLine("========================================");
Console.WriteLine(" SharpCoreDB Event Sourcing Demo");
Console.WriteLine(" Order Management System");
Console.WriteLine("========================================");
Console.WriteLine();

// Create event store
var eventStore = new InMemoryEventStore();

// Demo 1: Create and evolve an order
Console.WriteLine("=== Demo 1: Create and Evolve Order ===");
Console.WriteLine();

var orderId = "ORDER-001";
var customerId = "CUST-123";

// Create order
var order = OrderAggregate.CreateOrder(orderId, customerId, [
    new OrderItem { ProductId = "PROD-1", ProductName = "Laptop", Quantity = 1, Price = 999.99m },
    new OrderItem { ProductId = "PROD-2", ProductName = "Mouse", Quantity = 2, Price = 29.99m }
]);

Console.WriteLine($"Created order {orderId} for customer {customerId}");
Console.WriteLine($"Initial items: {order.Items.Count}, Total: ${order.TotalAmount:F2}");
await PersistEvents(eventStore, orderId, order);

// Add item
order.AddItem("PROD-3", "Keyboard", 1, 79.99m);
Console.WriteLine($"Added keyboard, New total: ${order.TotalAmount:F2}");
await PersistEvents(eventStore, orderId, order);

// Confirm order
order.ConfirmOrder();
Console.WriteLine($"Order confirmed, Status: {order.Status}");
await PersistEvents(eventStore, orderId, order);

// Mark as paid
order.MarkAsPaid(order.TotalAmount, "CreditCard", "TXN-789");
Console.WriteLine($"Payment received, Status: {order.Status}");
await PersistEvents(eventStore, orderId, order);

// Ship order
order.ShipOrder("TRACK-123", "FedEx", DateTimeOffset.UtcNow.AddDays(3));
Console.WriteLine($"Order shipped with tracking: {order.TrackingNumber}, Status: {order.Status}");
await PersistEvents(eventStore, orderId, order);

// Deliver order
order.MarkAsDelivered("John Doe");
Console.WriteLine($"Order delivered at {order.DeliveredAt:yyyy-MM-dd HH:mm}, Status: {order.Status}");
await PersistEvents(eventStore, orderId, order);

Console.WriteLine();

// Demo 2: Replay events to rebuild state
Console.WriteLine("=== Demo 2: Rebuild State from Events ===");
Console.WriteLine();

var streamId = new EventStreamId(orderId);
var events = await eventStore.ReadStreamAsync(streamId, new EventReadRange(1, long.MaxValue));

Console.WriteLine($"Reading {events.Events.Count} events from stream '{orderId}':");
foreach (var evt in events.Events)
{
    Console.WriteLine($"  [{evt.Sequence}] {evt.EventType} @ {evt.TimestampUtc:yyyy-MM-dd HH:mm:ss}");
}

var rebuiltOrder = OrderAggregate.FromEventStream(events.Events);
Console.WriteLine();
Console.WriteLine($"Rebuilt Order State:");
Console.WriteLine($"  Order ID: {rebuiltOrder.OrderId}");
Console.WriteLine($"  Customer: {rebuiltOrder.CustomerId}");
Console.WriteLine($"  Status: {rebuiltOrder.Status}");
Console.WriteLine($"  Items: {rebuiltOrder.Items.Count}");
Console.WriteLine($"  Total: ${rebuiltOrder.TotalAmount:F2}");
Console.WriteLine($"  Tracking: {rebuiltOrder.TrackingNumber}");
Console.WriteLine($"  Delivered: {rebuiltOrder.DeliveredAt:yyyy-MM-dd HH:mm}");
Console.WriteLine($"  Version: {rebuiltOrder.Version}");
Console.WriteLine();

// Demo 3: Create multiple orders and read global feed
Console.WriteLine("=== Demo 3: Multiple Orders & Global Feed ===");
Console.WriteLine();

// Create order 2
var order2 = OrderAggregate.CreateOrder("ORDER-002", "CUST-456", [
    new OrderItem { ProductId = "PROD-4", ProductName = "Monitor", Quantity = 1, Price = 299.99m }
]);
order2.ConfirmOrder();
order2.MarkAsPaid(order2.TotalAmount, "PayPal", "TXN-999");
await PersistEvents(eventStore, "ORDER-002", order2);

// Create order 3
var order3 = OrderAggregate.CreateOrder("ORDER-003", "CUST-789", [
    new OrderItem { ProductId = "PROD-5", ProductName = "Webcam", Quantity = 1, Price = 89.99m }
]);
order3.CancelOrder("Customer changed mind", "System");
await PersistEvents(eventStore, "ORDER-003", order3);

Console.WriteLine("Created 2 more orders");
Console.WriteLine();

// Read global event feed
var globalFeed = await eventStore.ReadAllAsync(1, 100);
Console.WriteLine($"Global Event Feed ({globalFeed.TotalCount} total events):");
Console.WriteLine();

var groupedByStream = globalFeed.Events.GroupBy(e => e.StreamId.Value);
foreach (var group in groupedByStream)
{
    Console.WriteLine($"Stream: {group.Key}");
    foreach (var evt in group)
    {
        Console.WriteLine($"  [Global:{evt.GlobalSequence}, Stream:{evt.Sequence}] {evt.EventType}");
    }
    Console.WriteLine();
}

// Demo 4: Point-in-time query
Console.WriteLine("=== Demo 4: Point-in-Time Query ===");
Console.WriteLine();

Console.WriteLine("Rebuilding ORDER-001 at sequence 3 (before payment):");
var partialEvents = await eventStore.ReadStreamAsync(
    new EventStreamId("ORDER-001"),
    new EventReadRange(1, 3)
);

var orderAtPoint = OrderAggregate.FromEventStream(partialEvents.Events);
Console.WriteLine($"  Status at sequence 3: {orderAtPoint.Status}");
Console.WriteLine($"  Total: ${orderAtPoint.TotalAmount:F2}");
Console.WriteLine($"  (Current status: {rebuiltOrder.Status})");
Console.WriteLine();

// Demo 5: Stream statistics
Console.WriteLine("=== Demo 5: Stream Statistics ===");
Console.WriteLine();

var streams = new[] { "ORDER-001", "ORDER-002", "ORDER-003" };
foreach (var streamName in streams)
{
    var length = await eventStore.GetStreamLengthAsync(new EventStreamId(streamName));
    Console.WriteLine($"{streamName}: {length} events");
}

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine(" Demo Complete!");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Key Concepts Demonstrated:");
Console.WriteLine("  ✅ Event sourcing with immutable events");
Console.WriteLine("  ✅ State reconstruction from event stream");
Console.WriteLine("  ✅ Command pattern (Create, Add, Confirm, etc.)");
Console.WriteLine("  ✅ Event replay and versioning");
Console.WriteLine("  ✅ Global event feed for projections");
Console.WriteLine("  ✅ Point-in-time queries");
Console.WriteLine("  ✅ Per-stream sequence tracking");
Console.WriteLine();

// Helper method to persist pending events
static async Task PersistEvents(IEventStore store, string orderId, OrderAggregate order)
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
