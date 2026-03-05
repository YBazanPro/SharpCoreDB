# Event Sourcing RFC for SharpCoreDB
## Low-Level Primitives and Optional Package Model

**Status:** RFC (Request for Comments)  
**Proposal Date:** 2026-03-03  
**Target:** Issue #55 - Native Event Sourcing Support  
**Implementation Window:** Weeks 3-12 (Milestones M2-M4)

---

## Executive Summary

SharpCoreDB.EventSourcing is a low-level, optional package that provides append-only event stream primitives for applications requiring event sourcing patterns. It is **not** a CQRS framework, aggregate engine, or opinionated orchestration layer. It is a set of contracts and in-memory/storage-backed implementations that enforce immutable append-only semantics, per-stream ordering, and global event feeds for projection-friendly reads.

**Key Design Principle:** *Keep the core SharpCoreDB lean by packaging event sourcing optionally.*

---

## 1. Problem Statement

### Current State

SharpCoreDB users can simulate event sourcing by storing events in a regular table, but this approach:
- Lacks semantic guarantees (append-only is a convention, not enforced)
- Requires manual sequence management per stream
- Offers no built-in global ordering for projections
- Lacks snapshot primitives to optimize replay cost

### Why SharpCoreDB Fits

SharpCoreDB already has:
- Append-only storage modes (WAL, immutable blocks)
- Transactions and isolation guarantees
- Efficient bulk operations
- Embedded/local deployment model (event stores like EventStoreDB are external)

A native, lightweight event sourcing layer makes SharpCoreDB a compelling all-in-one solution for applications that need:
- Relational data + event log in a single file
- Vector search (RAG) alongside event streams
- Local, embedded event storage without external dependencies

---

## 2. Goals

1. **Append-only semantics:** Events cannot be modified or deleted; only appended.
2. **Per-stream ordering:** Each stream has an independent sequence counter that never decreases.
3. **Global ordering:** All events across all streams have a global sequence for projection-friendly reads.
4. **Lightweight snapshots:** Optional snapshots per stream to reduce replay cost.
5. **Low-level primitives:** No framework overhead; application controls orchestration.
6. **Optional packaging:** Event sourcing does not impact SharpCoreDB core; can be ignored.
7. **Performance:** Append and read operations scale efficiently for 1M+ events per stream.

---

## 3. Non-Goals (Explicitly Out of Scope)

- **Aggregates framework:** No `IAggregateRoot` or `ICommand` abstractions.
- **Opinionated CQRS:** No `ICommandHandler` or `IQueryHandler` base classes.
- **Event versioning/upcasting:** Apps handle schema evolution.
- **Sagas/Choreography:** Apps compose workflows.
- **Projections framework:** No projection engine; read models are app responsibility.
- **Retention policies:** Apps decide when/how to snapshot or archive.
- **Distributed event sourcing:** Local/embedded focus only.

---

## 4. Contracts and Semantics

### 4.1 Event Stream Identity

```csharp
public readonly record struct EventStreamId(string Value);
```

- Unique logical identifier for a stream (e.g., `"user-123"`, `"order-456"`).
- Case-sensitive.
- Immutable.

### 4.2 Append Entry

```csharp
public readonly record struct EventAppendEntry(
    string EventType,
    ReadOnlyMemory<byte> Payload,
    ReadOnlyMemory<byte> Metadata,
    DateTimeOffset TimestampUtc);
```

- **EventType:** Discriminator for the event (e.g., `"UserCreated"`, `"OrderPlaced"`).
- **Payload:** Serialized event data (JSON/binary/custom).
- **Metadata:** Correlation ID, user context, or other operational data.
- **TimestampUtc:** Client-provided timestamp (must be UTC).

**Semantics:**
- Immutable after append.
- Timestamp is informational; storage engine provides durability order.

### 4.3 Persisted Event Envelope

```csharp
public readonly record struct EventEnvelope(
    EventStreamId StreamId,
    long Sequence,              // Per-stream sequence (1, 2, 3, ...)
    long GlobalSequence,        // Global ordering (1, 2, 3, ... across all streams)
    string EventType,
    ReadOnlyMemory<byte> Payload,
    ReadOnlyMemory<byte> Metadata,
    DateTimeOffset TimestampUtc);
```

**Guarantees:**
- `Sequence` uniquely identifies the event within the stream and never decreases.
- `GlobalSequence` uniquely identifies the event across all streams and never decreases.
- Both are assigned at append time by the store.

### 4.4 Snapshot

```csharp
public readonly record struct EventSnapshot(
    EventStreamId StreamId,
    long Version,               // Stream sequence at which snapshot was taken
    ReadOnlyMemory<byte> SnapshotData,
    DateTimeOffset CreatedAtUtc);
```

**Purpose:**
- Reduce replay cost by providing a "checkpoint" at a known stream version.
- Apps decide when/how often to snapshot (not enforced by engine).
- Snapshots are mutable (can be replaced); events are immutable.

### 4.5 Read Range

```csharp
public readonly record struct EventReadRange(long FromSequence, long ToSequence);
```

- Inclusive range for stream reads.
- Example: `new EventReadRange(10, 20)` reads sequences 10 through 20.

---

## 5. Service Contract (IEventStore)

### Core Operations

```csharp
public interface IEventStore
{
    Task<AppendResult> AppendEventAsync(
        EventStreamId streamId,
        EventAppendEntry entry,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppendResult>> AppendEventsAsync(
        EventStreamId streamId,
        IEnumerable<EventAppendEntry> entries,
        CancellationToken cancellationToken = default);

    Task<ReadResult> ReadStreamAsync(
        EventStreamId streamId,
        EventReadRange range,
        CancellationToken cancellationToken = default);

    Task<ReadResult> ReadAllAsync(
        long fromGlobalSequence = 1,
        int limit = 1000,
        CancellationToken cancellationToken = default);

    Task<long> GetStreamLengthAsync(
        EventStreamId streamId,
        CancellationToken cancellationToken = default);
}
```

### Result Models

```csharp
public readonly record struct AppendResult(
    EventStreamId StreamId,
    long AppendedSequence,
    long GlobalSequence,
    bool Success);

public readonly record struct ReadResult(
    IReadOnlyList<EventEnvelope> Events,
    long TotalCount);
```

---

## 6. Implementations

### 6.1 InMemoryEventStore (Testing & Development)

- Locks on append to ensure sequence ordering.
- Suitable for unit tests, demos, development.
- **Not** persistent; data lost on process exit.

### 6.2 DatabaseEventStore (Future: Weeks 4-6)

- Persists to SharpCoreDB core via append-only table.
- Leverages SharpCoreDB transactions and WAL.
- Single-threaded append contention handled by DB transaction layer.
- Will be the recommended implementation for production.

---

## 7. Snapshot Primitives (Future: Week 5)

Snapshots are optional. Apps can:

1. **Never snapshot:** Always replay from event 1 (simplest, suitable for <100K events).
2. **Manual snapshots:** Apps explicitly call `SaveSnapshotAsync()` on a schedule or policy.
3. **Projection checkpoints:** Store the last processed global sequence for projections (stored separately, not in snapshots).

The engine provides:
- Efficient storage for snapshot data.
- Efficient reads by `StreamId + Version`.
- No enforcement of snapshotting policy; apps decide.

---

## 8. Projection-Friendly Design

### Global Ordered Reads

```csharp
// Read all events in global order, starting from global sequence 1
var result = await store.ReadAllAsync(fromGlobalSequence: 1, limit: 1000);

// Process for projection
var readModel = new Dictionary<string, object>();
foreach (var envelope in result.Events)
{
    // Apply event to read model
    ApplyEventToProjection(readModel, envelope);
    
    // Track checkpoint for resumption
    lastProcessedGlobalSequence = envelope.GlobalSequence;
}
```

**Benefits:**
- No need for central projection coordinator.
- Each projection can resume from its last checkpoint.
- Supports multiple projections over same event log.

---

## 9. Integration with SharpCoreDB Core

### Package Boundaries

- `SharpCoreDB` core: ✅ No event sourcing code.
- `SharpCoreDB.EventSourcing`: ✅ Optional package, separate NuGet.
- `SharpCoreDB.Server`: ✅ Can optionally expose event store via gRPC (separate adapter in future).

### Database Schema (DatabaseEventStore, Future)

The `DatabaseEventStore` will create:

```sql
-- Event log table (append-only)
CREATE TABLE _event_store (
    stream_id TEXT NOT NULL,
    sequence BIGINT NOT NULL,
    global_sequence BIGINT NOT NULL,
    event_type TEXT NOT NULL,
    payload BLOB NOT NULL,
    metadata BLOB,
    timestamp_utc DATETIME NOT NULL,
    PRIMARY KEY (stream_id, sequence)
);

-- Snapshots table (mutable)
CREATE TABLE _snapshots (
    stream_id TEXT NOT NULL,
    version BIGINT NOT NULL,
    snapshot_data BLOB NOT NULL,
    created_at_utc DATETIME NOT NULL,
    PRIMARY KEY (stream_id, version)
);

-- Projection checkpoints table (optional, app-managed)
CREATE TABLE _projection_checkpoints (
    projection_name TEXT NOT NULL,
    last_global_sequence BIGINT NOT NULL,
    updated_at_utc DATETIME NOT NULL,
    PRIMARY KEY (projection_name)
);
```

---

## 10. Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Append single event | <1ms (in-memory), <5ms (DB) | Per-stream locking |
| Append batch (100 events) | <10ms (in-memory), <50ms (DB) | Atomic transaction |
| Read stream (100 events) | <1ms | Indexed by stream_id, sequence |
| Read all (100 events) | <5ms | Indexed by global_sequence |
| Snapshot save | <5ms | Mutable, no append guarantee |
| Snapshot read | <1ms | Direct lookup |

---

## 11. Concurrency Model

### Append Semantics

- **Single writer per stream:** Only one thread can append to a given stream at a time.
- **Lock-free for different streams:** Multiple threads can append to different streams concurrently.
- **In-memory:** Lock per operation; database transactions handle isolation.

### Read Semantics

- **No locking:** Reads are lock-free and non-blocking.
- **Snapshot isolation:** Reads return consistent snapshots at a point in time (database provides MVCC if available).

---

## 12. Error Handling

### Append Failures

- **Success:** `AppendResult.Success == true`; sequences assigned.
- **Failure:** `AppendResult.Success == false`; reasons logged (disk full, corruption, etc.).

### Read Failures

- **Non-existent stream:** Returns empty `ReadResult`.
- **Invalid range:** Throws `ArgumentException`.
- **Storage error:** Throws `IOException` or domain-specific exception.

---

## 13. Observability

### Metrics (Future: Week 9)

- Append latency (p50, p99).
- Batch size distribution.
- Global sequence counter (for monitoring throughput).
- Snapshot save/load latency.

### Logging

- Event append (INFO).
- Snapshot operations (INFO).
- Errors and retries (WARN/ERROR).

---

## 14. Testing Strategy

### Unit Tests (Weeks 3-6)

- Contract validation (EventStreamId, timestamps, sequences).
- Append correctness (sequence ordering, atomicity).
- Read correctness (range filtering, global ordering).
- Snapshot save/load.
- Concurrency (multiple appends to same stream, different streams).

### Integration Tests (Weeks 7-10)

- DatabaseEventStore with SharpCoreDB.
- Multi-projection scenario.
- Large event log (1M+ events).
- Crash recovery (if applicable).

---

## 15. Roadmap

### Phase 1: MVP (Weeks 3-6)
- ✅ Append-only semantics
- ✅ Per-stream and global ordering
- ✅ InMemoryEventStore
- ✅ Basic unit tests

### Phase 2: Hardening (Weeks 7-10)
- DatabaseEventStore implementation
- Snapshot save/load
- Observability (metrics, logging)
- Integration tests
- User documentation

### Phase 3: Production (Weeks 11-12)
- Performance tuning
- Release candidate and stable release
- Migration guide for users
- Example applications

### Phase 4+: Future (Deferred)
- Server-side event store adapter (gRPC exposure)
- Retention/archival policies
- Event versioning helpers
- Distributed event log (if demand warrants)

---

## 16. FAQ

### Q: Why is this not a full CQRS framework?

**A:** SharpCoreDB's philosophy is to provide low-level, composable primitives. A full framework would impose architectural opinions and add weight. Developers can easily add their own aggregate, handler, or projection abstractions on top.

### Q: Can I use this with existing event stores like EventStoreDB?

**A:** Yes, SharpCoreDB.EventSourcing is local/embedded. You can still use external event stores if you prefer. This package is for users who want simplicity and don't need distributed event infrastructure.

### Q: How do I migrate from event table to EventSourcing?

**A:** Create an `EventEnvelope` for each row in your event table, assign sequence numbers, and bulk insert into the event store. A migration helper may be added in Phase 2.

### Q: Is versioning/upcasting built-in?

**A:** No. Apps handle versioning by including a version in the payload or metadata. Upcasting logic is app-specific.

### Q: Can I delete events?

**A:** No. Append-only is a core guarantee. Archival/retention is app-specific (e.g., truncate events older than N months via admin API).

---

## 17. Decision Records

### D1: Optional Package (Separate NuGet)

**Decision:** Ship event sourcing as `SharpCoreDB.EventSourcing`, not in core.  
**Rationale:** Keeps core lean; users not using event sourcing avoid dependency overhead.  
**Consequence:** Slightly more setup (install package), simpler mental model (opt-in feature).

### D2: No Aggregate Framework

**Decision:** Provide contracts, not orchestration.  
**Rationale:** CQRS is an architectural choice, not a requirement. Apps have different patterns (domain aggregates, sagas, policy machines).  
**Consequence:** More flexibility, more responsibility on app side.

### D3: In-Memory First, Database Second

**Decision:** Implement InMemoryEventStore first; DatabaseEventStore in Weeks 4-6.  
**Rationale:** In-memory is testable, decouples from storage decisions, reveals API issues early.  
**Consequence:** Users can unit test with in-memory before deploying database-backed version.

---

## 18. References

- Issue #55: https://github.com/MPCoreDeveloper/SharpCoreDB/issues/55
- Event Sourcing Pattern: https://martinfowler.com/eaaDev/EventSourcing.html
- CQRS Pattern: https://martinfowler.com/bliki/CQRS.html
- EventStoreDB: https://www.eventstore.com/
- Axon Framework: https://axoniq.io/

---

## Approval Gate (Week 2)

This RFC is approved when:
- [ ] Contracts are reviewed and locked.
- [ ] Non-goals are acknowledged.
- [ ] Team agrees on optional package boundary.
- [ ] Performance targets are realistic.
- [ ] Roadmap is feasible.

**Date Approved:** _______________  
**Approver:** _______________

---

**Next Step:** Finalize benchmark methodology (Issue #56) and lock design in Week 2.
