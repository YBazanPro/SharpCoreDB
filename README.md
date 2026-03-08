<div align="center">
  <img src="https://raw.githubusercontent.com/MPCoreDeveloper/SharpCoreDB/master/SharpCoreDB.jpg" alt="SharpCoreDB Logo" width="200"/>
  
  # SharpCoreDB
  
  **High-Performance Embedded Database for .NET 10**
  
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
  [![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
  [![NuGet](https://img.shields.io/badge/NuGet-1.4.1-blue.svg)](https://www.nuget.org/packages/SharpCoreDB)
  [![Build](https://img.shields.io/badge/Build-✅_Passing-brightgreen.svg)](https://github.com/MPCoreDeveloper/SharpCoreDB)
  [![Tests](https://img.shields.io/badge/Tests-1468+_Passing-brightgreen.svg)](https://github.com/MPCoreDeveloper/SharpCoreDB)
  [![C#](https://img.shields.io/badge/C%23-14-purple.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
</div>

---

## 📌 **Current Status — v1.4.1 (February 28, 2026)**

### ✅ **Production-Ready: ALL Phase 1-10 Features Complete (100%)**

**SharpCoreDB v1.4.1 delivers critical bug fixes, 60-80% metadata compression, and enterprise-scale distributed features.**

#### 🎉 **Major Milestone: All Core Features Complete**

**Phase 1-10 (100% Complete):** SharpCoreDB is now a **fully-featured, production-ready embedded database** with advanced analytics, vector search, graph algorithms, and distributed capabilities.

**Next Phase:** 📅 **Phase 11 - SharpCoreDB.Server** (v1.5.0, Q2 2026)  
Transform SharpCoreDB from embedded database into a **network-accessible database server** with gRPC, Binary Protocol, and HTTP REST API support.

**See roadmap:** `docs/server/PHASE11_IMPLEMENTATION_PLAN.md`

---

#### 🎯 Latest Release (v1.4.0 → v1.4.1)

- **🐛 Critical Bug Fixes**
  - Database reopen edge case fixed (graceful empty JSON handling)
  - Immediate metadata flush ensures durability
  - Enhanced error messages with JSON preview
  
- **📦 New Features**
  - Brotli compression for JSON metadata (60-80% size reduction)
  - Backward compatible format detection
  - Zero breaking changes
  
- **📊 Quality Metrics**
  - **1,468+ tests** (was 850+ in v1.3.5)
  - **100% backward compatible**
  - **All 10 phases production-ready**

---

#### 🚀 Complete Feature Set (Phases 1-10)

**Phase 10: Enterprise Distributed Features** ✅
- Multi-master replication with vector clocks (Phase 10.2)
- Distributed transactions with 2PC protocol (Phase 10.3)
- Dotmim.Sync integration for cloud sync (Phase 10.1)

**Phase 9: Advanced Analytics** ✅
- 100+ aggregate functions (COUNT, SUM, AVG, STDDEV, VARIANCE, PERCENTILE, CORRELATION)
- Window functions (ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD)
- **150-680x faster than SQLite**

**Phase 8: Vector Search** ✅
- HNSW indexing with SIMD acceleration
- **50-100x faster than SQLite**
- Production-tested with 10M+ vectors

**Phase 6: Graph Algorithms** ✅
- A* pathfinding (30-50% improvement)
- Graph traversal (BFS, DFS, bidirectional search)
- ROWREF data type for graph edges
- GRAPH_TRAVERSE() SQL function

**Phases 1-5: Core Engine** ✅
- Single-file encrypted database (AES-256-GCM)
- SQL support with advanced query optimization
- ACID transactions with WAL
- B-tree and hash indexing
- Full-text search
- SIMD-accelerated operations
- Memory pooling and JIT optimizations

---

#### 🎯 **Phase 11 Roadmap (v1.5.0 - Q2 2026)**

**SharpCoreDB.Server** - Network database server with multi-protocol support:

✅ **Planning Complete:**
- Full 6-week implementation roadmap
- gRPC protocol specifications (first-class citizen)
- Binary protocol design (PostgreSQL-compatible)
- HTTP REST API design
- 15 benchmark scenarios defined

**Key Features:**
- **gRPC Protocol** (PRIMARY) - Bidirectional streaming, type-safe
- **Binary Protocol** - PostgreSQL wire protocol compatibility
- **HTTP REST API** - Web browsers, simple integrations
- **Enterprise Security** - TLS/SSL, JWT auth, RBAC
- **Connection Pooling** - 1000+ concurrent connections
- **Cross-Platform** - Windows/Linux/macOS installers
- **Client Libraries** - .NET (ADO.NET-style), Python, JavaScript

**Documentation:**
- `docs/server/PHASE11_IMPLEMENTATION_PLAN.md` - Full roadmap
- `docs/server/PHASE11_GRPC_FIRST_CLASS.md` - gRPC specifications
- `docs/server/PHASE11_BENCHMARKS_PLAN.md` - Benchmark strategy
- `docs/PHASE11_READY_STATUS.md` - Execution readiness

---

#### 📦 Installation

```bash
# Core database (v1.4.1 - NOW WITH METADATA COMPRESSION!)
dotnet add package SharpCoreDB --version 1.4.1

# Distributed features (multi-master replication, 2PC transactions)
dotnet add package SharpCoreDB.Distributed --version 1.4.1

# Analytics engine (100+ aggregate & window functions)
dotnet add package SharpCoreDB.Analytics --version 1.4.1

# Vector search (HNSW indexing, semantic search)
dotnet add package SharpCoreDB.VectorSearch --version 1.4.1

# Sync integration (bidirectional sync with SQL Server/PostgreSQL/MySQL/SQLite)
dotnet add package SharpCoreDB.Provider.Sync --version 1.4.1

# Graph algorithms (A* pathfinding)
dotnet add package SharpCoreDB.Graph --version 1.4.1

# Optional integrations
dotnet add package SharpCoreDB.EntityFrameworkCore --version 1.4.1
dotnet add package SharpCoreDB.Extensions --version 1.4.1
dotame add package SharpCoreDB.Serilog.Sinks --version 1.4.1
```

---

## 🚀 **Performance Benchmarks**

| Operation | SharpCoreDB | SQLite | Delta |
|-----------|------------|--------|-------|
| Bulk Insert (1M rows) | 2.8s | 18.2s | **6.5x faster** |
| COUNT (1M rows) | 0.8ms | 544ms | **682x faster** |
| Window Functions | 15ms | 2.3s | **156x faster** |
| Vector Search (10M) | 1.2ms | 120ms | **100x faster** |
| Metadata Compression | 24KB → 5.8KB | N/A | **75% reduction** |

---

## 🎯 **Core Features**

### ✅ **Production-Ready Capabilities**
- Single-file encrypted database with AES-256-GCM
- Full SQL support with advanced query optimization
- ACID transactions with Write-Ahead Logging (WAL)
- Multi-version concurrency control (MVCC)
- Automatic indexing (B-tree and hash)

### 📊 **Analytics & Data Processing**
- 100+ aggregate functions
- Window functions for complex analysis
- Statistical analysis (STDDEV, VARIANCE, PERCENTILE, CORRELATION)
- **150-680x faster than SQLite** for analytics

### 🔍 **Vector & Semantic Search**
- HNSW indexing with SIMD acceleration
- Semantic similarity search
- **50-100x faster than SQLite**
- Production-tested with 10M+ vectors

### 🌐 **Enterprise Distributed Features**
- Multi-master replication across nodes
- Distributed transactions with 2PC protocol
- Bidirectional sync with cloud databases
- Automatic conflict resolution
- Vector clock-based causality tracking

### 📱 **Cross-Platform Support**
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)
- Android, iOS (via portable library)
- IoT/Embedded devices

---

## 💻 **Quick Start**

```csharp
using SharpCoreDB;

// Create encrypted database
var factory = new DatabaseFactory();
var db = factory.Create("myapp.scdb", "master-password");

// Create table and insert data
db.ExecuteSQL("CREATE TABLE users (id INT PRIMARY KEY, name TEXT)");
db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");

// Query with advanced analytics
var results = db.ExecuteQuery(
  "SELECT name, COUNT(*) as count FROM users GROUP BY name"
);

// Persist to disk
db.Flush();
```

---

## 📚 **Documentation**

- **[Full Documentation](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/INDEX.md)** - Complete feature guide
- **[v1.4.1 Improvements](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/storage/METADATA_IMPROVEMENTS_V1.4.1.md)** - Metadata compression & bug fixes
- **[Progression Report](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/PROGRESSION_V1.3.5_TO_V1.4.1.md)** - All changes since v1.3.5
- **[Release Checklist](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/RELEASE_CHECKLIST_V1.4.1.md)** - Production release guide
- **[Analytics Guide](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/analytics/README.md)** - 100+ functions explained
- **[Vector Search Guide](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/vectors/README.md)** - HNSW indexing guide
- **[Distributed Features](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/distributed/README.md)** - Multi-master replication
- **[Phase 11 Roadmap](https://github.com/MPCoreDeveloper/SharpCoreDB/blob/master/docs/server/PHASE11_IMPLEMENTATION_PLAN.md)** - Server transformation plan

---

## 🏆 **Why SharpCoreDB?**

✅ **Performance**: 6.5x faster than SQLite for bulk operations  
✅ **Security**: AES-256-GCM encryption built-in  
✅ **Modern**: .NET 10 + C# 14 with SIMD acceleration  
✅ **Enterprise Ready**: 1,468+ tests, production-proven  
✅ **Cross-Platform**: Windows, Linux, macOS, ARM64 native  
✅ **Zero Configuration**: Single-file deployment  
✅ **Advanced Features**: Analytics, vector search, distributed transactions  

---

## 📄 **License**

MIT License - See [LICENSE](LICENSE) file

---

**Latest Version:** 1.4.1 | **Release Date:** February 28, 2026 | **Status:** ✅ Production Ready

