# Full GitHub Sub-Issue Set for Issues `#55` and `#56` (Weeks 1-16)
## Copy-Ready Backlog for Execution

**Parent issues:** `#55`, `#56`  
**Primary priority window:** Weeks 1-12 (P0/P1)  
**Deferred server window:** Weeks 13-16 (P2)

---

## Global Labels

Use these labels consistently:
- `enhancement`
- `documentation`
- `priority:P0` / `priority:P1` / `priority:P2`
- `roadmap:weekX`
- `parent:#55` or `parent:#56`

---

## Week 1

1. `[Week1][#55] Create Event Sourcing RFC skeleton with goals and non-goals`
2. `[Week1][#55] Draft SQL/API contracts for append and ordered reads`
3. `[Week1][#55] Create ADR for optional package boundary`
4. `[Week1][#56] Draft benchmark methodology for BLite and Zvec`
5. `[Week1][#56] Define dataset/hardware/runtime matrix`
6. `[Week1][#55/#56] Create dependency board and ownership mapping`

**Done gate:** RFC + ADR + benchmark method baseline approved.

---

## Week 2

1. `[Week2][#55] Finalize event stream model and sequence semantics`
2. `[Week2][#55] Finalize snapshot primitive model and explicit non-goals`
3. `[Week2][#56] Finalize BLite benchmark scenario matrix`
4. `[Week2][#56] Finalize Zvec benchmark scenario matrix`
5. `[Week2][#55/#56] Freeze acceptance criteria and milestone checks`

**Done gate:** Milestone M1 design lock complete.

---

## Week 3

1. `[Week3][#55] Scaffold SharpCoreDB.EventSourcing project and package metadata`
2. `[Week3][#55] Implement append-only event primitive MVP`
3. `[Week3][#56] Scaffold benchmark harness under tests/benchmarks`
4. `[Week3][#56] Add benchmark harness CLI/config baseline`

**Done gate:** Package and harness scaffolding merged.

---

## Week 4

1. `[Week4][#55] Implement stream ordered read by sequence range`
2. `[Week4][#55] Implement global ordered event feed primitive`
3. `[Week4][#56] Implement BLite CRUD benchmark scenario`
4. `[Week4][#56] Implement BLite batch insert benchmark scenario`
5. `[Week4][#56] Run BLite baseline and publish raw artifacts`

**Done gate:** Stream/global reads implemented; BLite baseline available.

---

## Week 5

1. `[Week5][#55] Implement snapshot primitives for stream replay optimization`
2. `[Week5][#55] Add replay correctness and sequence invariant tests`
3. `[Week5][#56] Implement Zvec index build benchmark scenario`
4. `[Week5][#56] Implement Zvec top-k latency and QPS scenarios`
5. `[Week5][#56] Add recall-vs-latency measurement workflow`

**Done gate:** Snapshot MVP + Zvec scenario coverage complete.

---

## Week 6

1. `[Week6][#55] Publish SharpCoreDB.EventSourcing prerelease package`
2. `[Week6][#56] Publish benchmark report v1 (BLite + Zvec)`
3. `[Week6][#56] Add CI benchmark workflow (initial run path)`
4. `[Week6][#55/#56] Complete Milestone M2 acceptance review`

**Done gate:** Milestone M2 complete.

---

## Week 7

1. `[Week7][#55] Add concurrency hardening for per-stream append`
2. `[Week7][#55] Add docs examples for append/read/snapshot lifecycle`
3. `[Week7][#56] Add benchmark rerun scripts`
4. `[Week7][#56] Add standardized report generation format`

**Done gate:** Hardening and reporting automation v1 complete.

---

## Week 8

1. `[Week8][#55] Implement optional projection checkpoint helper primitives`
2. `[Week8][#56] Add memory profiling scenarios to benchmark suite`
3. `[Week8][#56] Add GC behavior scenarios to benchmark suite`
4. `[Week8][#56] Expand CI artifact retention for benchmark evidence`

**Done gate:** Projection helper + memory/GC benchmark coverage complete.

---

## Week 9

1. `[Week9][#55] Add structured logs for event-sourcing operations`
2. `[Week9][#55] Add counters/metrics for append and read paths`
3. `[Week9][#56] Implement long-running mixed macro benchmark`
4. `[Week9][#56] Publish tuning guide (default vs tuned)`

**Done gate:** Observability baseline + macro benchmark complete.

---

## Week 10

1. `[Week10][#55] Stabilize package and prepare release candidate`
2. `[Week10][#56] Validate benchmark reproducibility with clean-environment rerun`
3. `[Week10][#55] Finalize user docs and migration notes`
4. `[Week10][#55/#56] Complete Milestone M3 acceptance review`

**Done gate:** Milestone M3 complete.

---

## Week 11

1. `[Week11][#56] Publish benchmark report v2 with expanded analysis`
2. `[Week11][#56] Add fairness FAQ and interpretation caveats`
3. `[Week11][#55/#56] Perform final issue acceptance audit`

**Done gate:** Report v2 and audit findings published.

---

## Week 12

1. `[Week12][#55] Publish stable SharpCoreDB.EventSourcing release`
2. `[Week12][#55] Optional: publish SharpCoreDB.Server.EventSourcing adapter preview`
3. `[Week12][#55/#56] Post completion evidence in parent issues`
4. `[Week12][#55/#56] Complete Milestone M4 closure`

**Done gate:** P0/P1 goals closed.

---

## Week 13 (Deferred P2)

1. `[Week13][P2] Re-prioritize server backlog with post-P0 dependency map`
2. `[Week13][P2] Start non-blocking server performance improvements`
3. `[Week13][#56] Keep benchmark regression checks running`

**Done gate:** Deferred server work resumed without P0 regression.

---

## Week 14 (Deferred P2)

1. `[Week14][P2] Continue server endpoint improvements (non-breaking)`
2. `[Week14][P2] Continue protocol improvements (non-breaking)`
3. `[Week14][#56] Maintain benchmark automation health`

**Done gate:** Server progress with no compatibility regressions.

---

## Week 15 (Deferred P2)

1. `[Week15][P2] Integrate optional server adapter improvements`
2. `[Week15][P2] Validate no mandatory coupling to server core`
3. `[Week15][#55] Verify optionality contract still holds after integration`

**Done gate:** Optionality contract remains intact.

---

## Week 16 (Deferred P2)

1. `[Week16][P2] Publish revised server milestones and release forecast`
2. `[Week16][#55/#56] Run final compatibility check against shipped P0 deliverables`
3. `[Week16][P2] Complete Milestone M5 closure review`

**Done gate:** Milestone M5 complete.

---

## Standard Sub-Issue Body Template

Use this body for each sub-issue:

### Objective
Describe the intended output in one sentence.

### Checklist
- [ ] Implement task scope
- [ ] Add/adjust tests or validation evidence
- [ ] Update docs where applicable
- [ ] Link evidence in parent issue

### Done criteria
- [ ] Output is merged
- [ ] Evidence is attached
- [ ] Parent issue checklist updated

---

## Parent Issue Status Snippets

### Parent `#55`
`Week X sub-issues executed for event-sourcing optional package scope. Optionality contract remains enforced.`

### Parent `#56`
`Week X benchmark sub-issues executed. Methodology and artifacts remain reproducible and published.`
