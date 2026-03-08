/**
 * Type definitions for @sharpcoredb/client
 */

/**
 * Connection configuration options
 */
export interface ConnectionOptions {
  /** Database name to connect to */
  database?: string;
  /** Username for authentication */
  username?: string;
  /** Password for authentication */
  password?: string;
  /** Whether to use TLS/SSL */
  tls?: boolean;
  /** Connection timeout in milliseconds */
  timeout?: number;
  /** Preferred protocols in order of preference */
  preferredProtocols?: ('grpc' | 'http' | 'websocket')[];
}

/**
 * Connection pool configuration options
 */
export interface PoolOptions extends ConnectionOptions {
  /** Minimum number of connections to maintain */
  minConnections?: number;
  /** Maximum number of connections allowed */
  maxConnections?: number;
  /** Maximum idle time before closing connection (ms) */
  maxIdleTime?: number;
  /** Maximum lifetime of a connection (ms) */
  maxLifetime?: number;
  /** Timeout for acquiring connection from pool (ms) */
  acquireTimeout?: number;
}

/**
 * Column metadata
 */
export interface Column {
  /** Column name */
  name: string;
  /** Column data type */
  type: string;
  /** Whether the column is nullable */
  nullable: boolean;
  /** Maximum length (for string types) */
  maxLength?: number;
}

/**
 * A single row in a query result
 */
export interface Row {
  /** Row values indexed by column position */
  [index: number]: any;
  /** Row values indexed by column name (if available) */
  [columnName: string]: any;
}

/**
 * Query execution result
 */
export interface QueryResult {
  /** Result columns */
  columns: Column[];
  /** Result rows */
  rows: Row[];
  /** Number of rows returned */
  rowCount: number;
  /** Query execution time in milliseconds */
  executionTimeMs: number;
  /** Whether there are more results available */
  hasMore?: boolean;
}

/**
 * Connection information
 */
export interface ConnectionInfo {
  /** Connected database name */
  databaseName: string;
  /** Session ID (if applicable) */
  sessionId?: string;
  /** Server version */
  serverVersion?: string;
  /** Connection establishment time */
  connectedAt?: Date;
}

/**
 * Query execution parameters
 */
export type QueryParameters = Record<string, any>;

/**
 * Callback function type for async operations
 */
export type Callback<T> = (error: Error | null, result?: T) => void;

/**
 * Protocol types supported by the client
 */
export type Protocol = 'grpc' | 'http' | 'websocket';

/**
 * Connection state
 */
export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'error';
