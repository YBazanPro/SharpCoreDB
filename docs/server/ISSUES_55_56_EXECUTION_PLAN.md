# SharpCoreDB Execution Plan for Issues `#55` and `#56`
## Week-by-Week Backlog, Milestones, and Release Gates

**Date:** 2026-03-03  
**Scope:** Execution detail for roadmap priorities (Issue `#55` + Issue `#56`)  
**Priority Model:** P0 user-facing issues first, server mode later  
**Constraint:** New features remain optional; event sourcing ships as separate NuGet package

---

## 1. Delivery Streams

## Stream A (P0): Issue `#55` — Event Sourcing as Optional Package
- Package: `SharpCoreDB.EventSourcing`
- Optional server adapter: `SharpCoreDB.Server.EventSourcing`
- No mandatory dependency added to `SharpCoreDB` core package

## Stream B (P0): Issue `#56` — Benchmark Program vs BLite and Zvec
- Reproducible benchmark harness under `tests/benchmarks`
- Public methodology and result documentation
- CI rerun capability for transparency

## Stream C (P2): Deferred Server Mode Expansion
- Starts after P0/P1 milestones are complete
- Must not block issue `#55` or issue `#56`

---

## 2. Work Breakdown by Week (16 Weeks)

## Weeks 1-2: Design Lock and Baseline

### Week 1
- Create Event Sourcing RFC with SQL/API contract draft
- Create benchmark methodology draft (datasets, hardware matrix, warm-up policy)
- Define package boundary ADR for optional feature compliance
- Create issue task board with dependency mapping

### Week 2
- Finalize event stream model (append, read, global feed, sequence semantics)
- Finalize snapshot primitive model and non-goals
- Finalize benchmark scenario matrix:
  - BLite track: CRUD, batch insert, filtered query, mixed workload
  - Zvec track: index build, top-k latency, QPS, recall trade-offs
- Freeze acceptance criteria for both issues

**Milestone M1:** Design lock complete

---

## Weeks 3-6: Event Sourcing MVP + Benchmark v1 (Parallel)

### Week 3
- Scaffold `SharpCoreDB.EventSourcing` project and package metadata
- Implement append-only event primitives
- Scaffold benchmark harness structure in `tests/benchmarks`

### Week 4
- Implement stream ordered reads by sequence range
- Implement global ordered event feed primitive
- Implement BLite benchmark scenarios and baseline runs

### Week 5
- Implement snapshot primitives (`StreamId`, `Version`, `SnapshotData`, `CreatedAt`)
- Add replay correctness tests and sequence invariants
- Implement Zvec benchmark scenarios and baseline runs

### Week 6
- Publish `SharpCoreDB.EventSourcing` prerelease package
- Publish Benchmark Report v1 with raw data references
- Add initial CI benchmark execution workflow

**Milestone M2:** Issue `#55` MVP + Issue `#56` Report v1 complete

---

## Weeks 7-10: Hardening and Production Readiness

### Week 7
- Add concurrency hardening for per-stream append behavior
- Add benchmark rerun scripts and standardized report generator
- Add documentation examples for append/read/snapshot lifecycle

### Week 8
- Add projection checkpoint helper primitives (optional only)
- Add memory and GC profiling scenarios to benchmark suite
- Expand CI validation with result artifact retention

### Week 9
- Add structured logging and counters for event-sourcing operations
- Add macro benchmark scenario (long-running mixed workload)
- Publish tuning guidance (default vs tuned settings)

### Week 10
- Package stabilization and release candidate for `SharpCoreDB.EventSourcing`
- Validation pass on benchmark reproducibility by fresh environment rerun
- Finalize user docs and migration notes

**Milestone M3:** Event sourcing release candidate + benchmark suite hardened

---

## Weeks 11-12: Final P1 Completion

### Week 11
- Publish Benchmark Report v2 (expanded analysis)
- Add FAQ and caveats for fair comparison interpretation
- Final issue acceptance audit for `#55` and `#56`

### Week 12
- Publish stable release of `SharpCoreDB.EventSourcing` (if RC gates pass)
- Optional: publish `SharpCoreDB.Server.EventSourcing` adapter preview
- Complete completion evidence checklist in issue threads

**Milestone M4:** P0/P1 issue goals complete

---

## Weeks 13-16: Deferred Server Mode Re-entry (P2)

### Week 13
- Re-prioritize server backlog with updated dependency map
- Start non-blocking server performance improvements

### Week 14
- Continue server endpoint and protocol improvements (non-breaking)
- Maintain benchmark automation and regression checks

### Week 15
- Integrate optional event-sourcing server adapter improvements
- Validate no mandatory coupling introduced in server core

### Week 16
- Publish revised server milestone plan and release forecast
- Perform final compatibility check against P0 deliverables

**Milestone M5:** Server re-entry complete without impacting shipped issue work

---

## 3. Acceptance Gates

## Gate A — Optional Feature Compliance (Issue `#55`)
- `SharpCoreDB` core package builds and runs without event-sourcing package
- Event sourcing package is independently installable
- Server adapter remains optional and separate from core package
- No hidden transitive dependency forcing event sourcing

## Gate B — Functional Correctness (Issue `#55`)
- Append-only semantics validated
- Per-stream ordering validated
- Global ordered read validated
- Snapshot create/read flows validated

## Gate C — Benchmark Integrity (Issue `#56`)
- Methodology published
- Hardware/runtime/dataset documented
- Raw benchmark outputs retained
- CI rerun produces consistent trend results

## Gate D — Documentation Quality
- English-only documentation
- Clear distinction between measured data and interpretation
- Known limitations and caveats included

---

## 4. Dependencies and Ownership Model

## Dependencies
- Stream A depends on core storage APIs and sequence consistency support
- Stream B depends on stable benchmark datasets and repeatable environments
- Stream C depends on completion of Stream A and Stream B P0/P1 milestones

## Suggested Ownership
- 3 engineers: Stream A (`#55`)
- 2 engineers: Stream B (`#56`)
- 1 engineer: cross-cutting CI/maintenance

---

## 5. Issue Traceability Matrix

| Item | Issue | Output |
|---|---|---|
| Event store primitives | `#55` | `SharpCoreDB.EventSourcing` API + tests |
| Snapshot primitives | `#55` | Snapshot model + replay optimization docs |
| Projection-friendly reads | `#55` | Stream/global read primitives |
| BLite benchmark track | `#56` | CRUD/batch/query comparative report |
| Zvec benchmark track | `#56` | Index/latency/QPS/recall comparative report |
| Reproducibility pipeline | `#56` | CI benchmark rerun workflow + artifacts |

---

## 6. Immediate Next Actions

1. Open sub-issues for Week 1 tasks under `#55` and `#56`.
2. Create package scaffold for `SharpCoreDB.EventSourcing`.
3. Create benchmark harness folder and initial project under `tests/benchmarks`.
4. Add milestone labels (`M1`..`M5`) in project tracking.
5. Start Week 1 implementation with daily progress summaries.

### Week 1 sub-issue templates
- Use `docs/server/WEEK1_SUBISSUES_55_56.md` for copy-ready GitHub issue templates and checklists.

### Full Weeks 1-16 sub-issue templates
- Use `docs/server/ALL_WEEKS_SUBISSUES_55_56.md` for the complete copy-ready sub-issue set across the full roadmap window.
