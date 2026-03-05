# SharpCoreDB.EventSourcing v1.5.0

Optional event sourcing package for SharpCoreDB.

## Included in this scaffold

- `EventStreamId`
- `EventAppendEntry`
- `EventEnvelope`
- `EventSnapshot`
- `EventReadRange`

## Design rules

- Append-only semantics at contract level
- Strong stream identity and per-stream sequence intent
- No mandatory coupling to server runtime
- Separate package boundary to preserve SharpCoreDB core optionality

## Installation

```bash
dotnet add package SharpCoreDB.EventSourcing --version 1.5.0
```
