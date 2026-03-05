# Order Management Persistent Demo

This example demonstrates `SharpCoreDbEventStore` persistence with SharpCoreDB as the backing database.

## What this demo proves

- Events are appended to a persistent SharpCoreDB table.
- Events survive process restart.
- Aggregate state can be rebuilt after reopening the store.

## Run

```bash
cd examples/EventSourcing/OrderManagement.PersistentDemo
dotnet run
```

## Expected behavior

1. Session 1 writes lifecycle events for one order.
2. Session 2 creates a new store instance with the same database path.
3. The second session reads the same events and rebuilds the aggregate.

If both sessions show the same event count, persistence is working.
