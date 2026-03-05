// <copyright file="Program.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using OrderManagement;
using SharpCoreDB;
using SharpCoreDB.EventSourcing;

Console.WriteLine("========================================");
Console.WriteLine(" SharpCoreDB Persistent Event Sourcing Demo");
Console.WriteLine(" Order Management with Database-Backed Store");
Console.WriteLine("========================================");
Console.WriteLine();

var dbPath = Path.Combine(Path.GetTempPath(), "SharpCoreDB", "orders-persistent-demo");
Console.WriteLine($"Database path: {dbPath}");
Console.WriteLine();

var orderId = "ORDER-PERSIST-001";
var customerId = "CUST-PERSIST-123";

Console.WriteLine("=== Session 1: Write Events to SharpCoreDB ===");
Console.WriteLine();

var writeStore = CreatePersistentStore(dbPath);

var order = OrderAggregate.CreateOrder(orderId, customerId, [
    new OrderItem { ProductId = "PROD-1", ProductName = "Laptop", Quantity = 1, Price = 999.99m },
    new OrderItem { ProductId = "PROD-2", ProductName = "Mouse", Quantity = 1, Price = 29.99m },
]);

await PersistEvents(writeStore, orderId, order);
order.ConfirmOrder();
await PersistEvents(writeStore, orderId, order);
order.MarkAsPaid(order.TotalAmount, "CreditCard", "TXN-PERSIST-001");
await PersistEvents(writeStore, orderId, order);

var beforeRestart = await writeStore.ReadStreamAsync(new EventStreamId(orderId), new EventReadRange(1, long.MaxValue));
Console.WriteLine($"Events written before restart: {beforeRestart.Events.Count}");
Console.WriteLine();

Console.WriteLine("=== Session 2: Reopen Store and Replay ===");
Console.WriteLine();

var readStore = CreatePersistentStore(dbPath);
var afterRestart = await readStore.ReadStreamAsync(new EventStreamId(orderId), new EventReadRange(1, long.MaxValue));

Console.WriteLine($"Events loaded after restart: {afterRestart.Events.Count}");
foreach (var evt in afterRestart.Events)
{
    Console.WriteLine($"  [{evt.Sequence}] {evt.EventType} (global:{evt.GlobalSequence})");
}

var rebuiltOrder = OrderAggregate.FromEventStream(afterRestart.Events);
Console.WriteLine();
Console.WriteLine("Rebuilt aggregate state:");
Console.WriteLine($"  Order ID: {rebuiltOrder.OrderId}");
Console.WriteLine($"  Customer: {rebuiltOrder.CustomerId}");
Console.WriteLine($"  Status: {rebuiltOrder.Status}");
Console.WriteLine($"  Total: ${rebuiltOrder.TotalAmount:F2}");
Console.WriteLine($"  Version: {rebuiltOrder.Version}");
Console.WriteLine();

Console.WriteLine("✅ Persistent event sourcing works: events survived process restart.");

static IEventStore CreatePersistentStore(string dbPath)
{
    var services = new ServiceCollection();
    services.AddSharpCoreDB();

    var serviceProvider = services.BuildServiceProvider();
    var factory = serviceProvider.GetRequiredService<DatabaseFactory>();
    var database = factory.Create(dbPath, "demo-password");

    return new SharpCoreDbEventStore(database);
}

static async Task PersistEvents(IEventStore store, string orderId, OrderAggregate order)
{
    var streamId = new EventStreamId(orderId);

    foreach (var orderEvent in order.PendingEvents)
    {
        var entry = new EventAppendEntry(
            EventType: orderEvent.GetType().Name,
            Payload: orderEvent.Serialize(),
            Metadata: Array.Empty<byte>(),
            TimestampUtc: orderEvent.Timestamp);

        await store.AppendEventAsync(streamId, entry);
    }

    order.ClearPendingEvents();
}
