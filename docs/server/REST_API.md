# SharpCoreDB Server — REST API Reference

**Base URL:** `https://localhost:8443/api/v1`  
**Protocol:** HTTPS only (TLS 1.2+)  
**Auth:** JWT Bearer token (except health endpoints)  
**Content-Type:** `application/json`

---

## Authentication

All endpoints (except health) require a JWT Bearer token:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Endpoints

### POST `/api/v1/query`

Execute a SELECT query and return results as JSON.

**Request:**

```json
{
  "sql": "SELECT * FROM users WHERE age > 25",
  "database": "appdb",
  "parameters": {
    "@minAge": 25
  },
  "timeoutMs": 30000
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `sql` | string | ✅ | — | SQL SELECT statement |
| `database` | string | — | `master` | Target database |
| `parameters` | object | — | `null` | Named query parameters |
| `timeoutMs` | int | — | `30000` | Query timeout (ms) |

**Response (200):**

```json
{
  "columns": [
    { "name": "id", "type": "Int32", "nullable": true },
    { "name": "name", "type": "String", "nullable": true },
    { "name": "age", "type": "Int32", "nullable": true }
  ],
  "rows": [
    [2, "Bob", 28],
    [3, "Charlie", 35]
  ],
  "rowsAffected": 2,
  "executionTimeMs": 1.23
}
```

---

### POST `/api/v1/execute`

Execute a non-query SQL statement (INSERT, UPDATE, DELETE, CREATE TABLE, etc.).

**Request:**

```json
{
  "sql": "INSERT INTO users VALUES (4, 'Dave', 'dave@example.com')",
  "database": "appdb"
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `sql` | string | ✅ | — | SQL statement |
| `database` | string | — | `master` | Target database |
| `parameters` | object | — | `null` | Named parameters |
| `timeoutMs` | int | — | `30000` | Timeout (ms) |

**Response (200):**

```json
{
  "rowsAffected": 0,
  "executionTimeMs": 0.87
}
```

---

### POST `/api/v1/batch`

Execute multiple SQL statements in a single request. Uses `ExecuteBatchSQL` for storage engine persistence.

**Request:**

```json
{
  "database": "appdb",
  "statements": [
    "INSERT INTO users VALUES (5, 'Eve', 'eve@example.com')",
    "INSERT INTO users VALUES (6, 'Frank', 'frank@example.com')",
    "INSERT INTO users VALUES (7, 'Grace', 'grace@example.com')"
  ],
  "transactional": false
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `statements` | string[] | ✅ | — | SQL statements to execute |
| `database` | string | — | `master` | Target database |
| `transactional` | bool | — | `false` | Wrap in transaction |

**Response (200):**

```json
{
  "statementsExecuted": 3,
  "totalExecutionTimeMs": 2.15,
  "success": true
}
```

**Error (400):**

```json
{
  "error": "No statements",
  "code": "EMPTY_BATCH",
  "details": "Batch requires at least one statement"
}
```

---

### GET `/api/v1/schema?database=appdb`

Get database schema information (tables and columns).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `database` | query string | — | `master` | Database name |

**Response (200):**

```json
{
  "database": "appdb",
  "tables": [
    {
      "name": "users",
      "columns": [
        { "name": "id", "type": "INTEGER", "nullable": true },
        { "name": "name", "type": "TEXT", "nullable": true },
        { "name": "email", "type": "TEXT", "nullable": true }
      ],
      "rowCount": 150
    }
  ],
  "lastUpdated": "2026-03-07T14:30:00Z"
}
```

---

### GET `/api/v1/databases`

List all hosted databases.

**Response (200):**

```json
{
  "databases": [
    { "name": "master", "isSystemDatabase": true, "isReadOnly": false },
    { "name": "model", "isSystemDatabase": true, "isReadOnly": true },
    { "name": "appdb", "isSystemDatabase": false, "isReadOnly": false }
  ]
}
```

---

### GET `/api/v1/health`

Server health status. **No authentication required.**

**Response (200):**

```json
{
  "status": "healthy",
  "timestamp": "2026-03-07T14:30:00Z",
  "version": "1.6.0",
  "activeConnections": 12,
  "totalDatabases": 5,
  "uptime": "3.08:45:12"
}
```

---

### GET `/api/v1/health/detailed`

Detailed server diagnostics. **No authentication required.**

**Response (200):**

```json
{
  "status": "Healthy",
  "uptimeSeconds": 284712.5,
  "activeSessions": 12,
  "totalDatabases": 5,
  "gcHeapSizeMb": 128.5,
  "totalMemoryBytes": 536870912,
  "databaseHealth": {
    "master": "OK",
    "appdb": "OK"
  }
}
```

---

### GET `/api/v1/metrics`

Server metrics for monitoring systems.

**Response (200):**

```json
{
  "timestamp": "2026-03-07T14:30:00Z",
  "activeConnections": 12,
  "totalConnections": 0,
  "memoryUsageMb": 128.5,
  "cpuUsagePercent": 0,
  "queriesPerSecond": 0,
  "databaseMetrics": {
    "master": { "name": "master", "sizeMb": 0, "connectionCount": 0 },
    "appdb": { "name": "appdb", "sizeMb": 0, "connectionCount": 0 }
  }
}
```

---

## Error Responses

All error responses follow the same format:

```json
{
  "error": "Human-readable error message",
  "code": "ERROR_CODE",
  "details": "Additional context (optional)"
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `DATABASE_NOT_FOUND` | 400 | Specified database does not exist |
| `EMPTY_BATCH` | 400 | Batch request has no statements |
| `QUERY_ERROR` | 500 | SQL query execution failed |
| `EXECUTE_ERROR` | 500 | SQL statement execution failed |
| `BATCH_ERROR` | 500 | Batch execution failed |
| `SCHEMA_ERROR` | 500 | Schema retrieval failed |

### HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Bad request (invalid input) |
| 401 | Unauthorized (missing/invalid JWT) |
| 429 | Too Many Requests (rate limited) |
| 500 | Server error |

---

## curl Examples

```bash
# Health check (no auth)
curl -fsk https://localhost:8443/api/v1/health

# List databases
curl -fsk https://localhost:8443/api/v1/databases \
  -H "Authorization: Bearer $TOKEN"

# Create table
curl -fsk -X POST https://localhost:8443/api/v1/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sql": "CREATE TABLE products (id INTEGER, name TEXT, price REAL)", "database": "appdb"}'

# Insert data
curl -fsk -X POST https://localhost:8443/api/v1/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sql": "INSERT INTO products VALUES (1, '\''Laptop'\'', 999.99)", "database": "appdb"}'

# Query
curl -fsk -X POST https://localhost:8443/api/v1/query \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sql": "SELECT * FROM products", "database": "appdb"}'

# Schema
curl -fsk "https://localhost:8443/api/v1/schema?database=appdb" \
  -H "Authorization: Bearer $TOKEN"
```

---

**See Also:** [Quick Start](QUICKSTART.md) · [Client Guide](CLIENT_GUIDE.md) · [Security Guide](SECURITY.md)
