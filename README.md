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

## 📌 **Current Status — v1.4.1 (March 8, 2026)**

### ✅ **Production-Ready: ALL Phase 1-11 Features Complete (100%)**

**SharpCoreDB v1.4.1 delivers critical bug fixes, 60-80% metadata compression, enterprise-scale distributed features, and a fully functional network database server.**

#### 🎉 **Major Milestone: All Core Features + Server Complete**

**Phase 1-11 (100% Complete):** SharpCoreDB is now a **fully-featured, production-ready embedded AND networked database** with advanced analytics, vector search, graph algorithms, distributed capabilities, and server mode.

**Latest Achievement:** 🚀 **Phase 11 - SharpCoreDB.Server COMPLETE (100%)**  
SharpCoreDB has been successfully transformed from embedded database into a **network-accessible database server** with gRPC, Binary Protocol, HTTP REST API, and WebSocket streaming support.

**Server Features Delivered:**
- ✅ gRPC protocol (HTTP/2 + HTTP/3, primary protocol)
- ✅ Binary TCP protocol handler
- ✅ HTTPS REST API (DatabaseController)
- ✅ WebSocket streaming protocol (real-time query streaming)
- ✅ JWT authentication + Role-Based Access Control
- ✅ Mutual TLS (certificate-based authentication)
- ✅ Multi-database registry with system databases
- ✅ Connection pooling (1000+ concurrent connections)
- ✅ Health checks & metrics (Prometheus-compatible)
- ✅ .NET Client library (ADO.NET-style)
- ✅ Python client (PySharpDB - published to PyPI)
- ✅ JavaScript/TypeScript SDK (published to npm)
- ✅ Docker + Docker Compose deployment
- ✅ Cross-platform installers (Windows Service, Linux systemd)
- ✅ Complete server documentation and examples

**See documentation:** `docs/INDEX.md`

### 📚 Documentation Policy

- Canonical documentation entry points are `docs/INDEX.md` and `docs/README.md`.
- Topic-level canonical entry points are maintained under:
  - `docs/server/README.md`
  - `docs/scdb/README_INDEX.md`
  - `docs/graphrag/00_START_HERE.md`
- Obsolete phase-status, kickoff, completion, and superseded planning documents are periodically removed.
- Historical snapshots are not treated as canonical product documentation.

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
  - **All 11 phases production-ready**

---

#### 🚀 Complete Feature Set (Phases 1-11)

**Phase 11: SharpCoreDB.Server (Network Database Server)** ✅
- gRPC protocol (HTTP/2 + HTTP/3) - primary, high-performance protocol
- Binary TCP protocol - PostgreSQL wire protocol compatibility
- HTTPS REST API - web browser and simple integration support
- WebSocket streaming - real-time query streaming
- JWT + Mutual TLS authentication
- Role-Based Access Control (Admin/Writer/Reader)
- Multi-database support with system databases
- Connection pooling (1000+ concurrent connections)
- Health checks & Prometheus metrics
- .NET, Python, JavaScript/TypeScript client libraries
- Docker + cross-platform installers (Windows/Linux)
- Complete documentation and examples

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

#### 📦 Installation

```bash
# Core database (v1.4.1 - NOW WITH METADATA COMPRESSION!)
dotnet add package SharpCoreDB --version 1.4.1

# Server mode (network database server with gRPC/HTTP/WebSocket)
dotnet add package SharpCoreDB.Server --version 1.4.1
dotnet add package SharpCoreDB.Client --version 1.4.1

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
dotten


