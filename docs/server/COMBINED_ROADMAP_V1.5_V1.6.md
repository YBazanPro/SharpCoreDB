# SharpCoreDB Unified Product Roadmap (Rebased)
## Integrated Plan for Existing Scope + Issues `#55` and `#56`

**Roadmap Date:** 2026-03-03  
**Planning Horizon:** 16 weeks  
**Primary Objective:** Deliver highest user-impact features as soon as possible.  
**Priority Rule:** User-requested features from open issues come before server-mode expansion.

---

## Executive Summary

This roadmap replaces the previous combined timeline with a user-first execution plan:

1. **Issue `#55` (Event Sourcing)** is moved to **highest priority** and delivered as an **optional add-on**.
2. **Issue `#56` (Benchmarks vs BLite/Zvec)** is also **highest priority** to provide transparent, reproducible performance evidence.
3. **Server mode work remains important but is intentionally lower priority** until the two user-facing issues are shipped.
4. All new capabilities follow SharpCoreDB modularity: **core stays lean, optional features ship in separate packages**.

---

## Non-Negotiable Product Constraints

### 1) Optional Feature Policy
- New features must be optional by default.
- Core engine must not force additional dependencies for users who do not need them.
- Feature toggles and package boundaries must be explicit.

### 2) Event Sourcing Packaging Policy (Issue `#55`)
- Event sourcing will be delivered as a separate NuGet package:
  - `SharpCoreDB.EventSourcing`
- Optional server integration will be separate:
  - `SharpCoreDB.Server.EventSourcing`
- `SharpCoreDB` core package remains usable without event sourcing components.

### 3) Security Baseline
- Existing server rule remains: HTTPS/TLS only (minimum TLS 1.2), no plain HTTP endpoints.

---

## Issue Analysis and Scope

## Issue `#55`: Native Event Sourcing Support
### Requested scope
- Append-only event store primitives
- Per-stream ordering and sequence handling
- Snapshot support to reduce replay cost
- Projection-friendly global ordered reads

### Delivery interpretation for SharpCoreDB
- Provide low-level primitives, not a full CQRS framework.
- Include SQL and API surface for append/read operations.
- Keep projection orchestration in application layer, with optional helpers only.

## Issue `#56`: Benchmark Against BLite and Zvec
### Requested scope
- Comparative benchmarks for relational/document and vector workloads.
- Include throughput, latency, memory usage, and quality trade-offs.

### Delivery interpretation for SharpCoreDB
- Publish reproducible benchmark suite and results.
- Separate benchmark scenarios by workload type:
  - BLite comparison: CRUD, batch insert, query, memory profile.
  - Zvec comparison: index build, top-k latency/QPS, recall/latency curves.

---

## Priority Model (Updated)

## P0 (Immediate)
- Issue `#55`: Optional Event Sourcing package
- Issue `#56`: Public benchmark program and reports

## P1 (After P0)
- Stabilization, docs hardening, sample apps, CI automation for benchmark reproducibility

## P2 (Lower Priority)
- Server mode expansion and advanced server-only enhancements
- Existing GraphRAG/server enhancements that are not blockers for P0/P1

---

## 16-Week Delivery Plan

## Phase A — Architecture and Baseline (Weeks 1-2)
**Goal:** Lock design and reproducibility standards before implementation.

### Deliverables
- Event sourcing RFC (`docs/server/EVENT_SOURCING_RFC.md`)
- Benchmark methodology RFC (`docs/benchmarks/BENCHMARK_METHOD.md`)
- Package split decision record (`docs/adr/ADR-EventSourcing-Package-Boundary.md`)
- Work breakdown and acceptance checklist for issues `#55` and `#56`

### Exit Criteria
- Package boundaries approved
- SQL/API proposal approved
- Benchmark datasets and hardware matrix frozen

---

## Phase B — Event Sourcing MVP (Weeks 3-6) [P0]
**Goal:** Ship usable optional event sourcing primitives quickly.

### Package Target
- `SharpCoreDB.EventSourcing` (NuGet prerelease in Week 6)

### Scope
- Append-only event stream model
- Stream sequence guarantees
- APIs for:
  - append single event
  - append batch events
  - read stream by sequence range
  - read global ordered event feed
- Snapshot storage primitives (minimal, efficient)

### Explicit Non-Goals (MVP)
- No aggregate framework
- No opinionated CQRS orchestration
- No mandatory coupling to server runtime

### Exit Criteria
- Functional API + SQL coverage for append/read/snapshot basics
- Replay correctness tests pass
- Package is installable and optional

---

## Phase C — Benchmark Track v1 (Weeks 3-7, parallel) [P0]
**Goal:** Produce first trusted benchmark report against BLite and Zvec.

### Deliverables
- Benchmark harness project under `tests/benchmarks/`
- Scenario packs:
  - BLite: CRUD, batch insert, filtered query, mixed read/write
  - Zvec: index build, top-k query latency, QPS, recall curves
- Report v1 in `docs/benchmarks/SHARPCOREDB_VS_BLITE_ZVEC_V1.md`

### Measurement Requirements
- Dataset sizes and warm-up policy documented
- Hardware + runtime versions pinned
- Raw output artifacts stored for auditability

### Exit Criteria
- Re-runnable benchmark scripts in CI
- Published results with methodology notes and caveats

---

## Phase D — Event Sourcing GA Hardening (Weeks 7-10) [P1]
**Goal:** Move optional package from MVP to production-grade baseline.

### Scope
- Concurrency/consistency validation for per-stream append
- Snapshot efficiency improvements
- Projection checkpoint helper primitives (still optional)
- Observability hooks (structured logs, counters)
- Additional docs + migration notes

### Optional Server Adapter
- `SharpCoreDB.Server.EventSourcing` for network exposure of event-sourcing APIs
- Server adapter remains secondary and does not block package release

### Exit Criteria
- `SharpCoreDB.EventSourcing` stable release candidate
- Integration tests for append/read/replay/snapshot scenarios

---

## Phase E — Benchmark Track v2 + Public Transparency (Weeks 8-11) [P1]
**Goal:** Publish expanded and defensible comparisons.

### Scope
- Add macro workload scenarios (long-running mixed workload)
- Add memory pressure and GC behavior analysis
- Add tuning profile section (default vs tuned)
- Publish reproducibility guide for community reruns

### Exit Criteria
- Report v2 published
- CI badge/automation for benchmark reruns operational

---

## Phase F — Server Mode Deferred Backlog Start (Weeks 12-16) [P2]
**Goal:** Resume server-mode improvements after user-priority items ship.

### Scope (deferred/lower priority)
- Server throughput optimization backlog
- Additional server endpoints and protocol enhancements
- Graph/server cross-feature refinements not required for issues `#55`/`#56`

### Exit Criteria
- Server backlog triaged with updated milestones
- No regression to completed P0/P1 deliverables

---

## Milestones

| Week | Milestone | Output |
|---|---|---|
| 2 | Design Lock | RFCs + ADR approved |
| 6 | Event Sourcing MVP | `SharpCoreDB.EventSourcing` prerelease |
| 7 | Benchmark v1 | First comparative report published |
| 10 | Event Sourcing RC | Hardened optional package |
| 11 | Benchmark v2 | Extended report + reproducibility guide |
| 16 | Server Re-entry | Lower-priority server backlog resumed |

---

## Acceptance Criteria by Issue

## Issue `#55` Done When
- Optional NuGet `SharpCoreDB.EventSourcing` released
- Append-only stream semantics validated
- Stream and global ordered read primitives available
- Snapshot primitives available
- Core package remains independent (no forced event-sourcing dependency)

## Issue `#56` Done When
- Benchmarks vs BLite and Zvec are publicly documented
- Methodology is reproducible and transparent
- Latency/throughput/memory/recall dimensions are covered
- CI-based rerun path exists for benchmark suites

---

## Risks and Mitigations

1. **Risk:** Scope creep into full CQRS framework  
   **Mitigation:** Keep package strictly primitive-focused; defer orchestration patterns.

2. **Risk:** Benchmark bias concerns  
   **Mitigation:** Publish raw data, exact configs, and reproducibility scripts.

3. **Risk:** Core package bloat  
   **Mitigation:** Enforce optional package boundaries and dependency checks in CI.

4. **Risk:** Server timeline pressure  
   **Mitigation:** Explicit P2 deferment accepted; protect P0/P1 delivery.

---

## Team Allocation (Recommended)

### Weeks 1-11 (User-first window)
- 3 engineers: Event sourcing package (`#55`)
- 2 engineers: Benchmark program (`#56`)
- 1 engineer: Core maintenance and regression prevention

### Weeks 12-16 (Server re-entry)
- 2 engineers: Continue event-sourcing support and fixes
- 2 engineers: Benchmark maintenance + automation
- 2 engineers: Server-mode deferred backlog

---

## What Changes Versus Previous Roadmap

- Server-first strategy is replaced with **issue-first strategy**.
- Event sourcing is now explicitly **optional** and **packaged separately**.
- Benchmarking becomes a first-class deliverable, not an afterthought.
- Server mode remains on roadmap, but priority is reduced until user-critical issues ship.

---

## Next Actions (Immediate)

1. Create implementation epics for `#55` and `#56` with week-based tasks.
2. Create package scaffolding for `SharpCoreDB.EventSourcing`.
3. Create benchmark harness in `tests/benchmarks`.
4. Publish RFC/ADR set and lock scope by end of Week 2.
5. Start parallel implementation and benchmark execution in Week 3.

## Detailed Execution Backlog
- See `docs/server/ISSUES_55_56_EXECUTION_PLAN.md` for the full week-by-week execution backlog, acceptance gates, and ownership model.

---

**Roadmap owner:** SharpCoreDB maintainers  
**Status:** Ready for execution
