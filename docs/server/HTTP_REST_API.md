# SharpCoreDB HTTP REST API (v1.5.0)

> Protocol priority: **gRPC is the flagship transport** for SharpCoreDB.Server.  
> The HTTP REST API is a secondary integration surface for simple clients, browser tooling, and operational workflows.

**Base URL:** `https://server:8443/api/v1`  
**Authentication:** JWT Bearer tokens or API keys  
**Content-Type:** `application/json`  
**Response Format:** JSON  

---

## Authentication

### JWT Authentication
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password",
  "database": "mydb"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_at": "2026-01-28T15:30:00Z",
  "user": "admin",
  "database": "mydb"
}
```

### API Key Authentication
```http
GET /api/v1/query?sql=SELECT%201
Authorization: Bearer sk-1234567890abcdef
```

---

## Query Execution

### Execute SQL Query
```http
POST /api/v1/query
Authorization: Bearer {token}
Content-Type: application/json

{
  "sql": "SELECT id, name FROM users WHERE age > ?",
  "parameters": [25],
  "timeout_ms": 30000,
  "streaming": false
}
```

**Response:**
```json
{
  "columns": [
    {"name": "id", "type": "INTEGER", "nullable": false},
    {"name": "name", "type": "STRING", "nullable": true}
  ],
  "rows": [
    [1, "Alice"],
    [2, "Bob"],
    [3, "Charlie"]
  ],
  "rows_affected": 3,
  "execution_time_ms": 15.5,
  "has_more": false
}
```

### Execute Non-Query (INSERT/UPDATE/DELETE)
```http
POST /api/v1/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "sql": "INSERT INTO users (name, email) VALUES (?, ?)",
  "parameters": ["Alice", "alice@example.com"]
}
```

**Response:**
```json
{
  "rows_affected": 1,
  "execution_time_ms": 8.2
}
```

### Batch Execute
```http
POST /api/v1/batch
Authorization: Bearer {token}
Content-Type: application/json

{
  "queries": [
    {
      "sql": "INSERT INTO users (name) VALUES (?)",
      "parameters": ["Alice"]
    },
    {
      "sql": "INSERT INTO users (name) VALUES (?)",
      "parameters": ["Bob"]
    }
  ],
  "transactional": true
}
```

**Response:**
```json
{
  "results": [
    {"success": true, "rows_affected": 1},
    {"success": true, "rows_affected": 1}
  ],
  "total_execution_time_ms": 12.3,
  "all_successful": true
}
```

---

## Transactions

### Begin Transaction
```http
POST /api/v1/transactions
Authorization: Bearer {token}
Content-Type: application/json

{
  "isolation_level": "READ_COMMITTED",
  "timeout_ms": 60000
}
```

**Response:**
```json
{
  "transaction_id": "tx_1234567890",
  "start_time": "2026-01-28T15:30:00Z"
}
```

### Execute in Transaction
```http
POST /api/v1/query
Authorization: Bearer {token}
Content-Type: application/json

{
  "sql": "UPDATE users SET balance = balance - 100 WHERE id = ?",
  "parameters": [1],
  "transaction_id": "tx_1234567890"
}
```

### Commit Transaction
```http
POST /api/v1/transactions/{transaction_id}/commit
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "duration_ms": 45.2
}
```

### Rollback Transaction
```http
POST /api/v1/transactions/{transaction_id}/rollback
Authorization: Bearer {token}
```

---

## Vector Search

### Vector Search
```http
POST /api/v1/vector/search
Authorization: Bearer {token}
Content-Type: application/json

{
  "table_name": "documents",
  "vector_column": "embedding",
  "query_vector": [0.1, 0.2, 0.3, 0.4, 0.5],
  "k": 10,
  "distance_function": "cosine",
  "filter_sql": "category = 'tech'",
  "include_distances": true,
  "include_vectors": false
}
```

**Response:**
```json
{
  "results": [
    {
      "row_id": 123,
      "distance": 0.123,
      "row_data": {
        "id": 123,
        "title": "AI Research Paper",
        "category": "tech"
      }
    }
  ],
  "search_time_ms": 5.2,
  "vectors_searched": 10000
}
```

### Batch Vector Search
```http
POST /api/v1/vector/search/batch
Authorization: Bearer {token}
Content-Type: application/json

{
  "searches": [
    {
      "table_name": "documents",
      "vector_column": "embedding",
      "query_vector": [0.1, 0.2, 0.3],
      "k": 5
    }
  ],
  "parallel": true
}
```

---

## Schema Operations

### Get Database Schema
```http
GET /api/v1/schema
Authorization: Bearer {token}
```

**Response:**
```json
{
  "tables": [
    {
      "name": "users",
      "columns": [
        {"name": "id", "type": "INTEGER", "nullable": false},
        {"name": "name", "type": "STRING", "nullable": true},
        {"name": "email", "type": "STRING", "nullable": true}
      ],
      "constraints": [
        {"name": "PK_users", "type": "PRIMARY_KEY", "columns": ["id"]}
      ]
    }
  ],
  "indexes": [
    {
      "name": "IX_users_email",
      "table_name": "users",
      "columns": ["email"],
      "is_unique": false,
      "type": "BTREE"
    }
  ]
}
```

### Create Table
```http
POST /api/v1/tables
Authorization: Bearer {token}
Content-Type: application/json

{
  "table_name": "products",
  "columns": [
    {"name": "id", "type": "INTEGER", "nullable": false},
    {"name": "name", "type": "STRING", "nullable": false, "max_length": 100},
    {"name": "price", "type": "DECIMAL", "nullable": false, "precision": 10, "scale": 2}
  ],
  "constraints": [
    {"name": "PK_products", "type": "PRIMARY_KEY", "columns": ["id"]}
  ]
}
```

**Response:**
```json
{
  "success": true,
  "table_name": "products"
}
```

### Create Index
```http
POST /api/v1/indexes
Authorization: Bearer {token}
Content-Type: application/json

{
  "index_name": "IX_users_email",
  "table_name": "users",
  "columns": ["email"],
  "type": "BTREE",
  "is_unique": false
}
```

---

## Graph Operations (GraphRAG)

### Graph Traversal
```http
POST /api/v1/graph/traverse
Authorization: Bearer {token}
Content-Type: application/json

{
  "start_table": "users",
  "start_row_id": 1,
  "relationship_column": "friend_refs",
  "direction": "OUTGOING",
  "max_depth": 3,
  "algorithm": "BFS",
  "include_paths": true,
  "filter_expression": "active = true"
}
```

**Response:**
```json
{
  "results": [
    {
      "table_name": "users",
      "row_id": 2,
      "depth": 1,
      "row_data": {"id": 2, "name": "Bob"}
    }
  ],
  "paths": [
    {
      "nodes": [
        {"table_name": "users", "row_id": 1},
        {"table_name": "users", "row_id": 2}
      ],
      "total_distance": 1.0
    }
  ],
  "execution_time_ms": 25.3
}
```

### Shortest Path
```http
POST /api/v1/graph/shortest-path
Authorization: Bearer {token}
Content-Type: application/json

{
  "start_table": "locations",
  "start_row_id": 1,
  "end_table": "locations",
  "end_row_id": 100,
  "relationship_column": "connections",
  "weight_expression": "distance",
  "algorithm": "DIJKSTRA"
}
```

---

## Analytics

### Execute Analytics Query
```http
POST /api/v1/analytics
Authorization: Bearer {token}
Content-Type: application/json

{
  "sql": "SELECT percentile(salary, 95), avg(salary), stddev(salary) FROM employees",
  "parallel_execution": true,
  "max_parallelism": 4
}
```

**Response:**
```json
{
  "columns": [
    {"name": "percentile", "type": "REAL"},
    {"name": "avg", "type": "REAL"},
    {"name": "stddev", "type": "REAL"}
  ],
  "rows": [[95000.0, 75000.0, 15000.0]],
  "execution_time_ms": 45.2,
  "metadata": {
    "rows_processed": 1000,
    "memory_used_bytes": 2048000,
    "parallel_executed": true,
    "parallelism_degree": 4
  }
}
```

---

## Health & Monitoring

### Health Check
```http
GET /api/v1/health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-28T15:30:00Z",
  "components": [
    {
      "name": "database",
      "status": "healthy",
      "message": "All databases accessible"
    },
    {
      "name": "connections",
      "status": "healthy",
      "message": "Connection pool healthy"
    }
  ]
}
```

### Server Information
```http
GET /api/v1/info
Authorization: Bearer {token}
```

**Response:**
```json
{
  "server_version": "1.5.0",
  "server_name": "SharpCoreDB-01",
  "startup_time": "2026-01-28T10:00:00Z",
  "active_connections": 15,
  "max_connections": 1000,
  "supported_features": ["vector", "graph", "analytics", "transactions"],
  "databases": [
    {
      "name": "master",
      "path": "/data/master.db",
      "size_bytes": 104857600,
      "is_system": true,
      "active_connections": 5
    }
  ],
  "stats": {
    "total_queries": 15420,
    "total_connections": 234,
    "uptime_seconds": 19845,
    "average_query_time_ms": 12.5
  }
}
```

### Metrics
```http
GET /api/v1/metrics
Authorization: Bearer {token}
```

**Response:**
```json
{
  "metrics": [
    {
      "name": "sharpcoredb_queries_total",
      "type": "COUNTER",
      "points": [
        {"timestamp": "2026-01-28T15:29:00Z", "value": 15420}
      ]
    },
    {
      "name": "sharpcoredb_connections_active",
      "type": "GAUGE",
      "points": [
        {"timestamp": "2026-01-28T15:29:00Z", "value": 15}
      ]
    }
  ]
}
```

---

## Error Handling

All errors follow this format:

```json
{
  "error": {
    "code": "INVALID_SQL",
    "message": "Syntax error in SQL query",
    "details": "Unexpected token 'SELCT' at position 1",
    "hint": "Did you mean 'SELECT'?",
    "position": 1
  },
  "request_id": "req_1234567890"
}
```

**Common Error Codes:**
- `INVALID_SQL`: SQL syntax error
- `AUTHENTICATION_FAILED`: Invalid credentials
- `PERMISSION_DENIED`: Insufficient permissions
- `CONNECTION_LIMIT_EXCEEDED`: Too many connections
- `TIMEOUT`: Query timed out
- `TRANSACTION_CONFLICT`: Transaction conflict

---

## Rate Limiting

API endpoints are rate limited:

- **Query endpoints:** 1000 requests/minute per client
- **Batch endpoints:** 100 requests/minute per client
- **Schema endpoints:** 60 requests/minute per client

Rate limit headers:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1643379660
```

---

## Content Negotiation

The API supports multiple response formats:

```http
GET /api/v1/query?sql=SELECT%201
Accept: application/xml
```

Supported formats:
- `application/json` (default)
- `application/xml`
- `text/csv`
- `application/octet-stream` (binary)

---

## Streaming Responses

Large result sets can be streamed:

```http
POST /api/v1/query
Content-Type: application/json
Accept: application/json-seq

{
  "sql": "SELECT * FROM large_table",
  "streaming": true
}
```

Returns JSON Lines format:
```
{"row": [1, "Alice"]}
{"row": [2, "Bob"]}
{"row": [3, "Charlie"]}
```

---

## WebSocket Support

Real-time query execution via WebSocket:

```javascript
const ws = new WebSocket('wss://server:8443/api/v1/ws');

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log('Query result:', data);
};

ws.send(JSON.stringify({
  type: 'query',
  sql: 'SELECT * FROM realtime_data',
  streaming: true
}));
```

---

**Last Updated:** January 28, 2026  
**API Version:** v1  
**Base URL:** `https://server:8443/api/v1`
