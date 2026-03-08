/**
 * @sharpcoredb/client - TypeScript/JavaScript client for SharpCoreDB Server
 *
 * A high-performance client library for SharpCoreDB Server with support for
 * gRPC, HTTP REST, and WebSocket protocols. Provides both callback-based and
 * Promise-based APIs with automatic connection pooling and protocol selection.
 *
 * @example
 * ```typescript
 * import { connect } from '@sharpcoredb/client';
 *
 * const connection = await connect('grpc://localhost:5001', {
 *   database: 'mydb'
 * });
 *
 * const result = await connection.execute('SELECT * FROM users');
 * console.log(`Found ${result.rows.length} users`);
 *
 * await connection.close();
 * ```
 */

export { Connection } from './connection';
export { connect } from './connection';
export { ConnectionPool, createPool } from './pool';

// Re-export types
export type {
  ConnectionOptions,
  QueryResult,
  Row,
  Column,
  ConnectionInfo,
  PoolOptions
} from './types';

// Re-export errors
export {
  SharpCoreDBError,
  ConnectionError,
  AuthenticationError,
  QueryError,
  ConfigurationError
} from './errors';

// Version
export const VERSION = '1.5.0';
