# @sharpcoredb/client

[![npm version](https://badge.fury.io/js/%40sharpcoredb%2Fclient.svg)](https://www.npmjs.com/package/@sharpcoredb/client)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0+-blue.svg)](https://www.typescriptlang.org/)
[![Node.js](https://img.shields.io/badge/Node.js-16+-green.svg)](https://nodejs.org/)

A high-performance TypeScript/JavaScript client library for [SharpCoreDB Server](https://github.com/MPCoreDeveloper/SharpCoreDB), providing both callback-based and Promise-based APIs with automatic protocol selection and connection pooling.

## Features

- 🚀 **High Performance**: Optimized for low-latency database operations
- 🔄 **Multiple Protocols**: gRPC (primary), HTTP REST, and WebSocket support
- 🔀 **Automatic Protocol Selection**: Automatically chooses the best available protocol
- 🏊 **Connection Pooling**: Efficient connection reuse and management
- ⚡ **Async/Await**: Full async support with Promises
- 🔒 **Type Safe**: Complete TypeScript definitions
- 📊 **Observability**: Built-in metrics and logging
- 🧪 **Well Tested**: Comprehensive test suite with Jest

## Installation

```bash
npm install @sharpcoredb/client
# or
yarn add @sharpcoredb/client
# or
pnpm add @sharpcoredb/client
```

## Quick Start

### TypeScript/ESM

```typescript
import { connect } from '@sharpcoredb/client';

async function main() {
  // Connect to SharpCoreDB Server
  const connection = await connect('grpc://localhost:5001', {
    database: 'mydb'
  });

  // Execute a query
  const result = await connection.execute('SELECT * FROM users WHERE age > ?', {
    age: 21
  });
  console.log(`Found ${result.rows.length} users`);

  // Insert data
  const affected = await connection.executeNonQuery(
    'INSERT INTO users (name, age) VALUES (?, ?)',
    { name: 'Alice', age: 30 }
  );
  console.log(`Inserted ${affected} rows`);

  // Ping the server
  const latency = await connection.ping();
  console.log(`Server latency: ${latency}ms`);

  await connection.close();
}

main().catch(console.error);
```

### JavaScript/CommonJS

```javascript
const { connect } = require('@sharpcoredb/client');

async function main() {
  const connection = await connect('grpc://localhost:5001', {
    database: 'mydb'
  });

  const result = await connection.execute('SELECT COUNT(*) as count FROM users');
  console.log(`Total users: ${result.rows[0].count}`);

  await connection.close();
}

main().catch(console.error);
```

### Connection Pooling

```typescript
import { createPool } from '@sharpcoredb/client';

async function main() {
  // Create a connection pool
  const pool = await createPool('localhost', 5001, {
    database: 'mydb',
    minConnections: 2,
    maxConnections: 10,
    maxIdleTime: 300000 // 5 minutes
  });

  // Use connections from the pool
  const connection = await pool.getConnection();
  try {
    const result = await connection.execute('SELECT * FROM large_table');
    console.log(`Fetched ${result.rows.length} rows`);
  } finally {
    await connection.close(); // Return to pool
  }

  await pool.close();
}

main().catch(console.error);
```

## API Reference

### Connection

The main connection class providing database operations.

#### Constructor

```typescript
new Connection(host: string, port: number, options?: ConnectionOptions)
```

#### Methods

- `connect(): Promise<void>` - Establish connection
- `close(): Promise<void>` - Close connection
- `execute(sql: string, parameters?: QueryParameters): Promise<QueryResult>` - Execute SELECT query
- `executeNonQuery(sql: string, parameters?: QueryParameters): Promise<number>` - Execute INSERT/UPDATE/DELETE
- `ping(): Promise<number>` - Test connection latency in milliseconds

#### Properties

- `isConnected: boolean` - Connection status
- `connectionInfo?: ConnectionInfo` - Connection metadata

#### Events

```typescript
connection.on('connected', (info: ConnectionInfo) => {
  console.log('Connected:', info);
});

connection.on('disconnected', (previousState: ConnectionState) => {
  console.log('Disconnected from:', previousState);
});

connection.on('error', (error: Error) => {
  console.error('Connection error:', error);
});
```

### Connection Pool

For high-concurrency applications.

#### Constructor

```typescript
new ConnectionPool(host: string, port: number, options?: PoolOptions)
```

#### Methods

- `getConnection(): Promise<PooledConnection>` - Get connection from pool
- `close(): Promise<void>` - Close pool and destroy all connections

#### Properties

- `stats: PoolStats` - Pool statistics
- `isClosed: boolean` - Pool status

### connect() Function

Convenience function for creating connections.

```typescript
connect(url: string, options?: ConnectionOptions): Promise<Connection>
```

**URL Formats:**
- `grpc://localhost:5001` - gRPC protocol
- `https://localhost:8443` - HTTP REST API
- `ws://localhost:8443` - WebSocket protocol

## Configuration

### ConnectionOptions

```typescript
interface ConnectionOptions {
  database?: string;        // Database name
  username?: string;        // Authentication username
  password?: string;        // Authentication password
  tls?: boolean;           // Use TLS (default: true)
  timeout?: number;        // Connection timeout in ms
  preferredProtocols?: Protocol[]; // Protocol preference order
}
```

### PoolOptions

```typescript
interface PoolOptions extends ConnectionOptions {
  minConnections?: number;  // Minimum pool size
  maxConnections?: number;  // Maximum pool size
  maxIdleTime?: number;     // Max idle time before closing (ms)
  maxLifetime?: number;     // Max connection lifetime (ms)
  acquireTimeout?: number;  // Timeout for acquiring connection (ms)
}
```

## Error Handling

```typescript
import {
  SharpCoreDBError,
  ConnectionError,
  AuthenticationError,
  QueryError,
  ConfigurationError
} from '@sharpcoredb/client';

try {
  const connection = await connect('grpc://localhost:5001');
  await connection.execute('SELECT * FROM nonexistent_table');
} catch (error) {
  if (error instanceof ConnectionError) {
    console.error('Connection failed:', error.message);
  } else if (error instanceof AuthenticationError) {
    console.error('Authentication failed:', error.message);
  } else if (error instanceof QueryError) {
    console.error('Query failed:', error.sql, error.message);
  } else {
    console.error('Unknown error:', error);
  }
}
```

## Performance Tuning

### Protocol Selection

- **gRPC**: Best for low-latency, high-throughput operations
- **HTTP**: Good for simple queries, easier firewall traversal
- **WebSocket**: Ideal for real-time streaming and subscriptions

### Connection Pooling

```typescript
// For high-concurrency apps
const pool = await createPool('localhost', 5001, {
  minConnections: 5,     // Keep warm connections
  maxConnections: 50,    // Allow burst capacity
  acquireTimeout: 10000, // Fail fast if pool exhausted
  maxIdleTime: 300000    // Close idle connections after 5 minutes
});
```

### Batch Operations

```typescript
// Execute multiple statements efficiently
const statements = [
  "INSERT INTO users VALUES ('Alice', 30)",
  "INSERT INTO users VALUES ('Bob', 25)",
  "INSERT INTO users VALUES ('Charlie', 35)"
];

// Note: Batch execution would be added in future versions
// For now, execute individually or use transactions
```

## Development

### Setup

```bash
# Clone repository
git clone https://github.com/MPCoreDeveloper/SharpCoreDB.git
cd SharpCoreDB/clients/javascript/sharpcoredb-client

# Install dependencies
npm install

# Build
npm run build

# Run tests
npm test

# Type checking
npm run typecheck

# Linting
npm run lint
```

### Testing

```bash
# Run all tests
npm test

# Run with coverage
npm run test:cov

# Run specific test
npm test -- connection.test.ts
```

### Building

```bash
# Development build
npm run dev

# Production build
npm run build

# Clean
npm run clean
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass: `npm test`
6. Ensure type checking passes: `npm run typecheck`
7. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](https://sharpcoredb.com/docs/js-client)
- 🐛 [Issue Tracker](https://github.com/MPCoreDeveloper/SharpCoreDB/issues)
- 💬 [Discussions](https://github.com/MPCoreDeveloper/SharpCoreDB/discussions)
- 📧 [Email Support](mailto:support@sharpcoredb.com)

---

**@sharpcoredb/client** is part of the [SharpCoreDB](https://github.com/MPCoreDeveloper/SharpCoreDB) ecosystem.
