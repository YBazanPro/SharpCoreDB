# SharpCoreDB.EventSourcing

Optional event sourcing primitives for SharpCoreDB.

## Scope

This package provides low-level contracts for:
- Append-only event entries
- Ordered stream reads
- Global ordered feed records
- Lightweight snapshots
- Per-stream sequence tracking
- Thread-safe in-memory implementation
- Persistent SharpCoreDB-backed implementation

## Non-goals

This package does **not** implement a full CQRS framework, aggregate orchestration, or opinionated projection pipelines.

## Install

```bash
dotnet add package SharpCoreDB.EventSourcing --version 1.5.0
```

## Optionality

`SharpCoreDB.EventSourcing` is intentionally separate from `SharpCoreDB` so users can keep the core dependency graph minimal when event sourcing is not needed.

---

## Quick Start

### 1. Create an Event Store

```csharp
using SharpCoreDB.EventSourcing;

// In-memory store for testing/lightweight scenarios
var inMemoryStore = new InMemoryEventStore();

// Persistent store backed by SharpCoreDB database
var persistentStore = new SharpCoreDbEventStore(database);
```

### 2. Append Events to a Stream

```csharp
// Define a stream
var streamId = new EventStreamId("order-12345");

// Create an event
var orderCreated = new EventAppendEntry(
    EventType: "OrderCreated",
    Payload: """{"orderId": "12345", "total": 99.99}"""u8.ToArray(),
    Metadata: """{"userId": "user-42"}"""u8.ToArray(),
    TimestampUtc: DateTimeOffset.UtcNow
);

// Append to stream
var result = await eventStore.AppendEventAsync(streamId, orderCreated);
Console.WriteLine($"Event appended at sequence {result.AppendedSequence}, global {result.GlobalSequence}");
```

### 3. Append Multiple Events (Batch)

```csharp
var events = new[]
{
    new EventAppendEntry("OrderCreated", payload1, metadata1, DateTimeOffset.UtcNow),
    new EventAppendEntry("OrderPaid", payload2, metadata2, DateTimeOffset.UtcNow),
    new EventAppendEntry("OrderShipped", payload3, metadata3, DateTimeOffset.UtcNow)
};

var results = await eventStore.AppendEventsAsync(streamId, events);
// All events assigned contiguous sequences atomically
```

### 4. Read Stream Events

```csharp
// Read events from sequence 1 to 100
var readResult = await eventStore.ReadStreamAsync(
    streamId,
    new EventReadRange(fromSequence: 1, toSequence: 100)
);

foreach (var envelope in readResult.Events)
{
    Console.WriteLine($"[{envelope.Sequence}] {envelope.EventType} @ {envelope.TimestampUtc}");
    // Process envelope.Payload, envelope.Metadata
}
```

### 5. Read All Events (Global Feed)

```csharp
// Read all events across all streams in global order
var allEvents = await eventStore.ReadAllAsync(
    fromGlobalSequence: 1,
    limit: 1000
);

Console.WriteLine($"Total events available: {allEvents.TotalCount}");
foreach (var evt in allEvents.Events)
{
    Console.WriteLine($"[Global {evt.GlobalSequence}] {evt.StreamId.Value} - {evt.EventType}");
}
```

### 6. Check Stream Length

```csharp
var length = await eventStore.GetStreamLengthAsync(streamId);
Console.WriteLine($"Stream has {length} events");
```

### 7. Save and Load Snapshots

```csharp
var snapshot = new EventSnapshot(
    StreamId: streamId,
    Version: currentVersion,
    SnapshotData: SerializeAggregate(state),
    CreatedAtUtc: DateTimeOffset.UtcNow);

await eventStore.SaveSnapshotAsync(snapshot);

var latest = await eventStore.LoadSnapshotAsync(streamId);
if (latest is { } loaded)
{
    Console.WriteLine($"Loaded snapshot at version {loaded.Version}");
}
```

---

## Core Concepts

### EventStreamId

Strongly-typed stream identifier. Each stream has independent sequence numbering.

```csharp
var streamId = new EventStreamId("aggregate-123");
```

### EventAppendEntry

Represents an event to be appended. Contains:
- `EventType`: string identifier (e.g., "OrderCreated")
- `Payload`: byte[] with serialized event data
- `Metadata`: byte[] for correlation/causation IDs
- `TimestampUtc`: when the event was created

### EventEnvelope

Returned when reading events. Adds:
- `Sequence`: per-stream sequence number (1-based, contiguous)
- `GlobalSequence`: global ordering across all streams

### Per-Stream Sequence

Each stream maintains its own sequence counter starting at 1. Concurrent appends to the same stream are serialized to ensure no gaps.

### Global Sequence

All events receive a global sequence number for ordered reading across streams. Useful for building projections and read models.

---

## Concurrency Guarantees

- **Thread-safe**: `InMemoryEventStore` uses lock-based synchronization
- **Atomic batch append**: All events in `AppendEventsAsync` succeed or none do
- **No gaps**: Stream sequences are always contiguous (1, 2, 3, ...)
- **Lock-free reads**: Read operations don't block writes

---

## Example: Complete Event Sourcing Flow

```csharp
using SharpCoreDB.EventSourcing;

// 1. Create store
var store = new InMemoryEventStore();
var orderId = new EventStreamId("order-999");

// 2. Append lifecycle events
await store.AppendEventAsync(orderId, 
    new EventAppendEntry("OrderCreated", GetPayload("created"), Array.Empty<byte>(), DateTimeOffset.UtcNow));
    
await store.AppendEventAsync(orderId,
    new EventAppendEntry("OrderPaid", GetPayload("paid"), Array.Empty<byte>(), DateTimeOffset.UtcNow));
    
await store.AppendEventAsync(orderId,
    new EventAppendEntry("OrderShipped", GetPayload("shipped"), Array.Empty<byte>(), DateTimeOffset.UtcNow));

// 3. Rebuild state from events
var events = await store.ReadStreamAsync(orderId, new EventReadRange(1, long.MaxValue));
var currentState = ReplayEvents(events.Events);
Console.WriteLine($"Order status: {currentState}");

// 4. Read global feed (all orders)
var globalFeed = await store.ReadAllAsync(1, 100);
Console.WriteLine($"{globalFeed.TotalCount} total events across all streams");

static byte[] GetPayload(string state) => System.Text.Encoding.UTF8.GetBytes($"{{\"state\":\"{state}\"}}");

static string ReplayEvents(IReadOnlyList<EventEnvelope> events)
{
    var state = "Unknown";
    foreach (var evt in events)
    {
        state = evt.EventType switch
        {
            "OrderCreated" => "Created",
            "OrderPaid" => "Paid",
            "OrderShipped" => "Shipped",
            _ => state
        };
    }
    return state;
}
```

---

## Performance Characteristics (InMemoryEventStore)

| Operation | Latency | Notes |
|-----------|---------|-------|
| Single append | < 1ms | Lock contention on same stream |
| Batch append (100) | < 10ms | Atomic transaction |
| Read stream (100) | < 1ms | No locks, direct list access |
| Read all (100) | < 5ms | Merges all streams |

---

## Persistence Notes

`InMemoryEventStore` is for **testing and lightweight scenarios only**. Data is lost when the process ends.

For persistent storage with SharpCoreDB, use `SharpCoreDbEventStore`.

Both stores now support snapshot persistence APIs:
- `SaveSnapshotAsync(EventSnapshot)`
- `LoadSnapshotAsync(EventStreamId, maxVersion)`

For other production backends, implement `IEventStore` with:
- Event Store DB
- PostgreSQL (jsonb columns)
- Azure Cosmos DB (change feed)

---

## Testing

This package includes 25+ unit tests covering:
- Append operations (single and batch)
- Concurrent appends (thread safety)
- Read operations (stream and global)
- Edge cases (empty streams, large batches, inverted ranges)
- Data integrity (sequence contiguity, payload preservation)

Run tests:
```bash
dotnet test tests/SharpCoreDB.EventSourcing.Tests
```

---

## References

- [Event Sourcing RFC](../../docs/server/EVENT_SOURCING_RFC.md)
- [Event Stream Model Spec](../../docs/server/EVENT_STREAM_MODEL_FINAL.md)
- [Issue #55 Acceptance Criteria](../../docs/server/ISSUE_55_ACCEPTANCE_CRITERIA.md)

---

## License

MIT License. See LICENSE file in repository root.
