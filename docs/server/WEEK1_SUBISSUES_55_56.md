# Week 1 GitHub Sub-Issues for `#55` and `#56`
## Copy-Ready Tasks and Checklists

**Parent issues:** `#55`, `#56`  
**Priority:** P0  
**Sprint window:** Week 1

> For the full Weeks 1-16 sub-issue set, see `docs/server/ALL_WEEKS_SUBISSUES_55_56.md`.

---

## Usage

- Create each item below as a separate GitHub issue.
- Add a backlink to the parent (`#55` or `#56`).
- Keep all checklists unchanged for consistent tracking.

---

## Sub-Issue 1 — Event Sourcing RFC skeleton and structure

**Parent:** `#55`  
**Suggested labels:** `enhancement`, `documentation`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#55] Create Event Sourcing RFC skeleton with scope, goals, and non-goals`

### Objective
Create the first RFC draft structure for native event-sourcing primitives, explicitly scoped to low-level capabilities only.

### Checklist
- [ ] Create `docs/server/EVENT_SOURCING_RFC.md`
- [ ] Add problem statement and motivation
- [ ] Add goals section
- [ ] Add explicit non-goals section (no full CQRS framework)
- [ ] Add terminology section (`StreamId`, `Sequence`, snapshots, projections)
- [ ] Add review owner section

### Done criteria
- RFC file exists with all required sections
- Scope clearly matches parent issue `#55`

---

## Sub-Issue 2 — SQL/API contract draft for event primitives

**Parent:** `#55`  
**Suggested labels:** `enhancement`, `documentation`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#55] Draft SQL and API contracts for append/read event primitives`

### Objective
Define initial SQL-like and API-level contracts for append-only stream operations and ordered reads.

### Checklist
- [ ] Draft append operation contract (single + batch)
- [ ] Draft stream read contract (range by sequence)
- [ ] Draft global ordered feed contract
- [ ] Define expected error model (conflict/validation)
- [ ] Add examples to RFC

### Done criteria
- Contract draft is documented in RFC
- Reviewers can implement Week 2 model decisions from this draft

---

## Sub-Issue 3 — ADR for optional package boundary

**Parent:** `#55`  
**Suggested labels:** `documentation`, `architecture`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#55] Create ADR for event sourcing package boundary and optionality`

### Objective
Formalize that event sourcing remains optional and ships via separate package(s) without coupling to core package.

### Checklist
- [ ] Create `docs/adr/ADR-EventSourcing-Package-Boundary.md`
- [ ] Record decision: `SharpCoreDB.EventSourcing` separate NuGet package
- [ ] Record optional server adapter strategy
- [ ] Record dependency constraints for `SharpCoreDB` core package
- [ ] Add consequences and trade-offs section

### Done criteria
- ADR approved by maintainers
- Optionality policy is explicit and testable

---

## Sub-Issue 4 — Benchmark methodology draft document

**Parent:** `#56`  
**Suggested labels:** `enhancement`, `documentation`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#56] Draft benchmark methodology for BLite and Zvec comparison`

### Objective
Create a reproducible benchmark methodology document for fair comparison across relational/document and vector workloads.

### Checklist
- [ ] Create `docs/benchmarks/BENCHMARK_METHOD.md`
- [ ] Define benchmark principles (fairness, repeatability, transparency)
- [ ] Define warm-up and run-count policy
- [ ] Define metrics (latency, throughput, memory, recall)
- [ ] Define reporting format

### Done criteria
- Methodology draft is complete and reviewable
- No scenario runs required yet

---

## Sub-Issue 5 — Dataset and hardware matrix specification

**Parent:** `#56`  
**Suggested labels:** `enhancement`, `documentation`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#56] Define benchmark dataset sizes and hardware/runtime matrix`

### Objective
Freeze all benchmark execution inputs needed for reproducibility.

### Checklist
- [ ] Define dataset sizes for BLite scenarios
- [ ] Define vector dataset sizes for Zvec scenarios
- [ ] Define hardware profile templates (CPU, RAM, storage)
- [ ] Define runtime versions (`.NET`, OS, package versions)
- [ ] Add storage location for raw benchmark artifacts

### Done criteria
- Inputs are frozen and documented
- Teams can run identical scenarios in Week 3+

---

## Sub-Issue 6 — Week 1 dependency board and sequencing

**Parent:** `#55`, `#56`  
**Suggested labels:** `project-management`, `priority:P0`, `roadmap:week1`

### Title
`[Week1][#55/#56] Create dependency board and task sequencing for P0 streams`

### Objective
Create execution sequencing so event-sourcing design and benchmark design can proceed in parallel without blockers.

### Checklist
- [ ] Define dependencies between sub-issues 1..5
- [ ] Add owners to each sub-issue
- [ ] Add target completion dates within Week 1
- [ ] Add blockers/escalation policy
- [ ] Add definition-of-done checklist at board level

### Done criteria
- Board exists and is linked from both parent issues
- Work can proceed in parallel with clear ownership

---

## Suggested Parent-Issue Update Snippets

### For `#55`
`Week 1 sub-issues created: RFC skeleton, SQL/API contract draft, optional package ADR. These items enforce optional packaging and non-framework scope.`

### For `#56`
`Week 1 sub-issues created: benchmark methodology and dataset/hardware matrix. This locks reproducibility before implementation starts.`

---

## Week 1 Exit Gate (M1 partial)

All of the following must be true:
- [ ] Event sourcing RFC skeleton exists and is reviewed
- [ ] SQL/API draft exists for append/read primitives
- [ ] Optional package ADR is approved
- [ ] Benchmark methodology draft exists and is reviewed
- [ ] Dataset/hardware matrix is frozen
- [ ] Dependency board is active with owners
