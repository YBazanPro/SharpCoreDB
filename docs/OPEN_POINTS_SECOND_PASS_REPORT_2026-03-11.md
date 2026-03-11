# Open Points Second-Pass Verification Report

**Date:** 2026-03-11  
**Scope:** Full repository second-pass audit after the `StorageMigrator` follow-up implementation  
**Repository:** `SharpCoreDB`

## 1. Method

This second-pass audit used a stricter approach than the earlier marker-only scan:

1. Scan `src` for hard markers: `TODO`, `FIXME`, `NotImplementedException`, `Future Milestone`
2. Scan `src` again for softer backlog indicators: `placeholder`, `for now`, `not yet implemented`, `not yet supported`
3. Review `tests`, `tools`, and `docs` separately to distinguish intentional stubs from real follow-up work
4. Manually inspect the most relevant code locations before classifying them

## 2. Executive Summary

### Main conclusion
There is **no longer a classic marker-based product backlog** in `src`.

However, there are still **four real product-code follow-up points** that should be treated as open work because they affect runtime behavior or correctness:

1. `AstExecutor.ExecuteSelect(...)` is still a stub that returns an empty result set
2. `IN (SELECT ...)` in AST execution is explicitly unsupported
3. `QueryCompiler` can silently drop a failed `WHERE` filter and continue unfiltered
4. `SqlParser.GraphTraversal` still uses sync-over-async via `.Result`

So the correct current status is:
- **Hard-marker backlog in `src`:** effectively resolved
- **Real remaining product follow-up work in `src`:** **4 items**

## 3. Product-Code Findings (`src`)

### 3.1 High priority

#### A. `src/SharpCoreDB/Services/SqlParser.DML.cs`
- `AstExecutor.ExecuteSelect(SelectNode selectNode)` still returns `[]`
- This is a real functional gap, not just a comment issue
- Severity: **High**

**Why it matters:** any code path relying on AST execution for SELECT can silently produce empty results instead of actual query output.

---

#### B. `src/SharpCoreDB/Services/SqlParser.InExpressionSupport.cs`
- `IN (SELECT ...)` throws `NotSupportedException`
- Message: `IN with subquery is not yet supported in AstExecutor. Use value lists for now.`
- Severity: **High**

**Why it matters:** this is a user-visible SQL compatibility gap.

---

### 3.2 Medium priority

#### C. `src/SharpCoreDB/Services/QueryCompiler.cs`
- If `WHERE` compilation fails, execution may continue without the filter
- Current comment indicates the query is intentionally allowed to run unfiltered
- Severity: **High for correctness / Medium for implementation effort**

**Why it matters:** this is potentially dangerous behavior because a filtered query may return more rows than requested.

---

#### D. `src/SharpCoreDB/Services/SqlParser.GraphTraversal.cs`
- Graph traversal uses `_graphTraversalProvider.TraverseAsync(...).Result`
- Severity: **Medium**

**Why it matters:** this is sync-over-async in product code and can become a blocking or deadlock risk depending on the call context.

---

### 3.3 Informational only

#### E. `src/SharpCoreDB/Services/SqlParser.DML.cs`
- `catch (NotImplementedException) { throw; }`
- This is not an unresolved implementation by itself
- Severity: **Informational**

## 4. Non-Product Findings

### 4.1 Tests
Most remaining `NotImplementedException` markers in `tests` are intentional mock/stub members inside test-only fake `ITable` implementations.

Primary examples:
- `tests/SharpCoreDB.Tests/CollationJoinTests.cs`
- `tests/SharpCoreDB.Benchmarks/Phase7_JoinCollationBenchmark.cs`

These are not product blockers.

### 4.2 Benchmarks
- `tests/benchmarks/SharpCoreDB.Benchmarks/Zvec/ZvecQueryBenchmark.cs`
- Contains explicit roadmap TODOs for benchmark scenarios

This is benchmark backlog, not core runtime backlog.

### 4.3 Tools
Low-priority tool-level stubs remain:
- `tools/SharpCoreDB.Viewer/Converters/DictionaryValueConverter.cs`
- `tools/SharpCoreDB.Viewer/Converters/ObjectToStringConverter.cs`
- `tools/SharpCoreDB.Viewer/Converters/LocalizeExtension.cs`

Each still throws in `ConvertBack(...)`. These are usually acceptable for one-way converters, unless two-way binding is intended.

Also:
- `tools/SharpCoreDB.DebugBenchmark/Program.cs` is explicitly a placeholder debug entry point

### 4.4 Documentation
Documentation is currently inconsistent:

- `docs/OPEN_POINTS_VERIFICATION_REPORT_2026-03-11.md` now understates remaining work because the second-pass audit found softer but still real product gaps
- `docs/PROJECT_STATUS.md` still points to older audit wording and should be refreshed to match the current narrower set of remaining issues

## 5. Recommended Next Actions

### Priority 1
1. Implement `AstExecutor.ExecuteSelect(...)`
2. Add support for `IN (SELECT ...)` in AST execution
3. Stop `QueryCompiler` from running unfiltered when `WHERE` compilation fails

### Priority 2
4. Remove sync-over-async from `SqlParser.GraphTraversal`

### Priority 3
5. Refresh `docs/PROJECT_STATUS.md`
6. Refresh or replace `docs/OPEN_POINTS_VERIFICATION_REPORT_2026-03-11.md`
7. Decide whether tool `ConvertBack(...)` methods should remain throwing or become explicit no-op/unsupported conversions

## 6. Final Assessment

A careful second pass shows that the repository is in a much better state than before, but it is **not accurate to say that all product-code follow-up work is finished**.

The remaining work is now **small and concentrated**, but it is still real:
- **4 product-code follow-up items in `src`**
- additional low-priority tool and documentation cleanup
