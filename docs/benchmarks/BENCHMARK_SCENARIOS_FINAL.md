# Benchmark Scenario Matrix (Final Lockdown)
## Week 2 Specification for BLite and Zvec Comparison

**Status:** Final Specification (Ready for Implementation)  
**Locked Date:** 2026-03-03  
**Applies to:** Issue #56 Benchmark Program

---

## 1. BLite Scenario Matrix (LOCKED)

### Scenario B1: Basic CRUD (100K Operations)

**Objective:** Measure insert, read, update, delete throughput and latency on a standard relational workload.

**Setup Phase:**
```json
{
  "database": "test-blite-b1.db",
  "table": "documents",
  "schema": {
    "id": "INTEGER PRIMARY KEY AUTOINCREMENT",
    "name": "TEXT NOT NULL (50-100 chars)",
    "email": "TEXT UNIQUE",
    "age": "INTEGER (18-100)",
    "score": "REAL (0-100)",
    "tags": "TEXT (JSON array)",
    "created_at": "DATETIME",
    "updated_at": "DATETIME",
    "is_active": "BOOLEAN (70% true)",
    "metadata": "TEXT (JSON object)"
  },
  "operations": 100000
}
```

**Execution Phases:**

| Phase | Operation | Count | Metric |
|-------|-----------|-------|--------|
| 1 | INSERT (single) | 100,000 | throughput (docs/sec), latency p50/p99 |
| 2 | SELECT by PK | 100,000 random IDs | latency p50/p99, QPS |
| 3 | SELECT with filter | 10,000 queries | latency (age > 25 AND age < 75), QPS |
| 4 | UPDATE | 10,000 random rows | throughput (rows/sec), latency |
| 5 | DELETE | 10,000 random rows | throughput (rows/sec), latency |
| 6 | SELECT all | 1 full table scan | total time, rows/sec |

**Measured Metrics:**
- Insert throughput: operations/sec
- Read latency: p50, p99, max (microseconds)
- Update/delete throughput: operations/sec
- Final row count: should be 80,000 (after 10K deletes)
- Final database size: MB
- Peak memory during test: MB

**Acceptance Criteria:**
- All phases complete without error
- Latency values recorded with sub-millisecond precision
- Memory tracking captures peak usage

---

### Scenario B2: Batch Insert (1M Documents)

**Objective:** Measure bulk load performance and memory efficiency.

**Setup:**
```json
{
  "database": "test-blite-b2.db",
  "total_documents": 1000000,
  "batch_sizes": [1000, 5000, 10000, 50000]
}
```

**Execution:**

For each batch size, perform:
1. Start timer
2. Insert batch of N documents in single transaction
3. Record latency per batch
4. Record memory snapshot
5. Repeat until 1M documents loaded

**Measured Metrics:**
- Throughput per batch size: docs/sec
- Memory growth curve: MB vs documents inserted
- Total load time: seconds
- Transactions per second (TPS)

**Measured Data Points:**
```
Batch Size 1,000:
  - Avg latency per batch: X ms
  - Throughput: Y docs/sec
  - Memory at 100K docs: M1 MB
  - Memory at 500K docs: M2 MB
  - Memory at 1M docs: M3 MB

[Repeat for 5K, 10K, 50K batch sizes]
```

**Acceptance Criteria:**
- All 4 batch sizes complete
- Memory recorded at 100K, 250K, 500K, 750K, 1M document marks
- Database file size recorded at end

---

### Scenario B3: Filtered Query Performance (1M Documents)

**Objective:** Measure indexed and unindexed query performance on large dataset.

**Setup:**
- Pre-populated with 1M documents from B2
- Column: `age` (integer, 18-100, uniformly distributed)
- Index: Create B-tree index on `age` column

**Execution:**

```sql
-- Query template (10,000 iterations)
SELECT * FROM documents WHERE age > ? AND age < ?

-- Generate random range queries
-- Examples: (20, 80), (25, 75), (30, 70), etc.
```

**Measured Metrics:**
- Query latency per query: microseconds (1000 data points minimum)
- Throughput: QPS
- Index size: MB
- Latency p50, p99, max

**Acceptance Criteria:**
- 10,000 queries executed
- Latency percentiles computed
- No timeout (< 100ms per query expected)

---

### Scenario B4: Mixed Workload (10-Minute Sustained Load)

**Objective:** Measure sustained performance under realistic mixed read/write load.

**Setup:**
- 500K pre-loaded documents
- 8 concurrent threads
- Mix: 60% read, 30% insert, 10% update

**Execution:**

```
Thread 1-8 (8 parallel):
  For 10 minutes:
    - 60% of operations: SELECT random key or range query
    - 30% of operations: INSERT new document
    - 10% of operations: UPDATE random document
  
  Record per-operation latency
  Track memory every 10 seconds
  Track throughput every minute
```

**Measured Metrics:**
- Overall throughput: ops/sec
- Latency percentiles (p50, p95, p99) for each operation type
- Memory over time: graph
- Tail latency stability (p99 variance)
- GC pause times (if instrumented)

**Acceptance Criteria:**
- All 8 threads run to completion (10 minutes)
- No crashes or deadlocks
- Throughput remains stable (variance < 10% across minutes)
- Memory does not exceed baseline + 500MB

---

## 2. Zvec Scenario Matrix (LOCKED)

### Scenario Z1: Index Build (1M Vectors, 768D)

**Objective:** Measure index construction time and memory overhead.

**Setup:**
```json
{
  "vectors": 1000000,
  "dimensions": 768,
  "vector_type": "float32",
  "distance_metric": "cosine",
  "index_types": ["hnsw", "brute_force"]
}
```

**Execution:**

**Phase 1: Brute-Force Baseline**
1. Load 1M vectors from dataset
2. Build brute-force index (linear scan)
3. Record build time, memory

**Phase 2: HNSW Index**
1. Load 1M vectors from dataset
2. Build HNSW index with parameters:
   - M (max connections): 16
   - ef_construction (construction parameter): 200
3. Record build time, memory, index size

**Measured Metrics:**
- Build time: seconds
- Index size: MB
- Memory peak: MB
- Vectors/sec throughput

**Acceptance Criteria:**
- Both index types complete
- Index files verified (can be loaded)
- Memory tracking captures peak usage

---

### Scenario Z2: Top-K Latency (1M Vectors, Warm Index)

**Objective:** Measure query latency for different K values.

**Setup:**
- 1M vectors indexed (warm: cached in memory)
- 1000 random query vectors from the dataset

**Execution:**

For each K value [10, 100, 1000]:
1. Warm cache (run 100 queries, discard results)
2. Execute 1000 top-k queries
3. Record latency per query
4. Compute percentiles

**Measured Metrics:**

```
K=10:
  - Latency p50: X ms
  - Latency p99: Y ms
  - QPS: Z queries/sec

K=100:
  - [same as above]

K=1000:
  - [same as above]
```

**Acceptance Criteria:**
- All K values tested
- Latency increases with K (as expected)
- 1000 queries per K value executed

---

### Scenario Z3: Throughput Under Load (1M Vectors, 8 Threads)

**Objective:** Measure sustained QPS under concurrent load.

**Setup:**
- 1M vectors indexed
- 8 concurrent threads
- Each thread: 100 top-k queries (k=10)

**Execution:**

```
Thread 1-8 (8 parallel):
  For i in 1..100:
    - Issue top-k query (k=10)
    - Record latency
  
  Track overall start/end time
  Compute total QPS = (8 * 100) / elapsed_seconds
```

**Measured Metrics:**
- Total throughput: QPS
- Per-thread latency (p50, p99)
- Tail latency (p99)

**Acceptance Criteria:**
- All threads complete
- Total throughput >= 1000 QPS (expected from single-threaded)
- Latency within expected range

---

### Scenario Z4: Recall vs Latency Tradeoff (1M Vectors)

**Objective:** Measure accuracy/speed tradeoff by varying search parameters.

**Setup:**
- 1M vectors indexed
- 100 query vectors with ground-truth (computed via brute-force)
- Vary HNSW parameter `ef_search`: [10, 50, 100, 200, 500]

**Execution:**

For each ef_search value:
1. Execute 100 top-k queries (k=10)
2. Compare results to ground-truth
3. Compute recall = (# correct results / 10)
4. Record latency

**Measured Metrics:**

```
ef_search=10:
  - Latency p50: A ms
  - Latency p99: B ms
  - Recall@10: C%

ef_search=50:
  - [same as above]

... [continue for 100, 200, 500]
```

**Acceptance Criteria:**
- All ef_search values tested
- Recall increases with ef_search (as expected)
- Latency increases with ef_search (as expected)
- Recall curve plotted

---

### Scenario Z5: Insert Performance (Incremental Add)

**Objective:** Measure cost of adding vectors to an existing index.

**Setup:**
- Start with empty index
- Pre-index 100K vectors
- Insert additional 900K vectors in batches

**Execution:**

```
For batch_size in [10000]:
  Repeat until 900K inserted:
    1. Insert batch of 10K vectors
    2. Record time
    3. Update index (if needed)
    4. Measure memory

Final: 1M vectors total
```

**Measured Metrics:**
- Insert throughput: vectors/sec
- Per-batch latency: ms
- Total insert time: seconds
- Memory growth: MB

**Acceptance Criteria:**
- All 900K vectors inserted
- Insert throughput measured
- Final index verified (readable)

---

## 3. Data Generation Specifications (LOCKED)

### BLite Document Generation

```python
# Pseudocode for dataset generation
import random

def generate_documents(count: int, seed: int = 42):
    random.seed(seed)
    names = ["Alice", "Bob", "Charlie", "Diana", ...]  # 1000 common names
    domains = ["gmail.com", "yahoo.com", "outlook.com", ...]  # 100 domains
    
    for i in range(count):
        yield {
            "id": i + 1,
            "name": random.choice(names) + str(random.randint(0, 9999)),
            "email": f"{random.choice(names)}{i}@{random.choice(domains)}",
            "age": random.randint(18, 100),
            "score": random.uniform(0, 100),
            "tags": random.sample(["tag1", "tag2", ..., "tag100"], k=random.randint(2, 5)),
            "created_at": datetime.now() - timedelta(days=random.randint(0, 365)),
            "updated_at": datetime.now() - timedelta(days=random.randint(0, 180)),
            "is_active": random.random() < 0.7,
            "metadata": {
                "source": random.choice(["web", "api", "import"]),
                "campaign": f"campaign-{random.randint(1, 50)}"
            }
        }
```

### Zvec Vector Generation

```python
import numpy as np

def generate_vectors(count: int, dimensions: int = 768, seed: int = 42):
    rng = np.random.RandomState(seed)
    vectors = rng.randn(count, dimensions).astype(np.float32)
    
    # Normalize for cosine distance
    vectors /= np.linalg.norm(vectors, axis=1, keepdims=True)
    
    return vectors
```

---

## 4. Environment Matrix (LOCKED)

### Primary Test Environment

```json
{
  "hardware": {
    "cpu": "Intel Core i9-13900K (24 threads)",
    "cores": 8,
    "memory": "64 GB DDR5",
    "storage": "1 TB NVMe SSD"
  },
  "os": {
    "name": "Windows 11",
    "build": "23H2"
  },
  "runtime": {
    "dotnet": "10.0.0",
    "java": "OpenJDK 21 LTS (if Zvec Java required)"
  }
}
```

### Secondary Environments (Optional, Clearly Labeled)

- Linux (Ubuntu 24.04)
- macOS (M3 ARM64)
- Small hardware (8GB RAM, quad-core)

Results from different environments are **not directly compared** in the main report.

---

## 5. Acceptance Criteria for Benchmark Suite (M1 Lockdown)

✅ **All items locked:**

1. [x] BLite scenarios (B1-B4) fully specified
2. [x] Zvec scenarios (Z1-Z5) fully specified
3. [x] Data generation scripts defined
4. [x] Metrics clearly defined (latency, throughput, recall)
5. [x] Environment matrix locked
6. [x] Execution policy (warm-up, run, cooldown) specified
7. [x] Reporting format defined
8. [x] Raw data export format specified

---

**Status:** ✅ **LOCKED FOR IMPLEMENTATION**

Week 3 begins scenario implementation and first benchmark runs on this specification.
