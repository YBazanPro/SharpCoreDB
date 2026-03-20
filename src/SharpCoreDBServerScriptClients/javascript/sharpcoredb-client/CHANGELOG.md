# @sharpcoredb/client Changelog

All notable changes to @sharpcoredb/client will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6.0] - 2026-01-28

### Added
- Initial release of @sharpcoredb/client
- Support for gRPC, HTTP REST, and WebSocket protocols
- Automatic protocol selection with fallback
- Connection pooling for high-performance applications
- Full TypeScript support with comprehensive type definitions
- Async/await support with Promises
- Event-driven connection lifecycle
- Comprehensive error handling and custom error classes
- Full test suite with Jest
- Documentation and examples

### Features
- **Multi-Protocol Support**: Connect via gRPC (recommended), HTTP, or WebSocket
- **Connection Pooling**: Efficient connection reuse with configurable limits
- **Type Safety**: Complete TypeScript definitions for all APIs
- **Automatic Failover**: Seamless protocol fallback if primary protocol unavailable
- **Event System**: Connection lifecycle events (connected, disconnected, error)
- **Performance Monitoring**: Built-in connection statistics and latency tracking

### Examples
```typescript
import { connect } from '@sharpcoredb/client';

const connection = await connect('grpc://localhost:5001', {
  database: 'mydb'
});

const result = await connection.execute('SELECT * FROM users');
console.log(`Found ${result.rows.length} users`);

await connection.close();
```

### Dependencies
- @grpc/grpc-js: ^1.9.0
- @grpc/proto-loader: ^0.7.0
- axios: ^1.6.0
- ws: ^8.14.0
- google-protobuf: ^3.21.0

## [Unreleased]

### Planned
- Streaming query results for large datasets
- Prepared statement support
- Transaction management
- Connection retry logic with exponential backoff
- Metrics collection and OpenTelemetry integration
- Node.js 18+ optimizations
- Binary protocol support
- Authentication token refresh
- Connection health monitoring
- Bulk operation helpers
- React hooks for React applications
- Browser bundle for web applications
