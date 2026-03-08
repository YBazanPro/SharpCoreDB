/**
 * Error classes for @sharpcoredb/client
 */

/**
 * Base error class for all SharpCoreDB client errors
 */
export class SharpCoreDBError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'SharpCoreDBError';
  }
}

/**
 * Error thrown when connection to server fails
 */
export class ConnectionError extends SharpCoreDBError {
  constructor(message: string, public readonly host?: string, public readonly port?: number) {
    super(message);
    this.name = 'ConnectionError';
  }
}

/**
 * Error thrown when authentication fails
 */
export class AuthenticationError extends SharpCoreDBError {
  constructor(message: string) {
    super(message);
    this.name = 'AuthenticationError';
  }
}

/**
 * Error thrown when a query execution fails
 */
export class QueryError extends SharpCoreDBError {
  constructor(
    message: string,
    public readonly sql?: string,
    public readonly parameters?: Record<string, any>,
    public readonly errorCode?: number
  ) {
    super(message);
    this.name = 'QueryError';
  }
}

/**
 * Error thrown when client configuration is invalid
 */
export class ConfigurationError extends SharpCoreDBError {
  constructor(message: string) {
    super(message);
    this.name = 'ConfigurationError';
  }
}

/**
 * Error thrown when an operation times out
 */
export class TimeoutError extends SharpCoreDBError {
  constructor(message: string, public readonly timeoutMs?: number) {
    super(message);
    this.name = 'TimeoutError';
  }
}
