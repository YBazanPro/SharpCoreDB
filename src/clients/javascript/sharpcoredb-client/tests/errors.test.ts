/**
 * @jest-environment node
 */

import {
  SharpCoreDBError,
  ConnectionError,
  AuthenticationError,
  QueryError,
  ConfigurationError
} from '../src/errors';

describe('Errors', () => {
  describe('SharpCoreDBError', () => {
    it('should create error with message', () => {
      const error = new SharpCoreDBError('Test error');
      expect(error.message).toBe('Test error');
      expect(error.name).toBe('SharpCoreDBError');
      expect(error).toBeInstanceOf(Error);
    });
  });

  describe('ConnectionError', () => {
    it('should create connection error', () => {
      const error = new ConnectionError('Connection failed', 'localhost', 5001);
      expect(error.message).toBe('Connection failed');
      expect(error.name).toBe('ConnectionError');
      expect(error.host).toBe('localhost');
      expect(error.port).toBe(5001);
      expect(error).toBeInstanceOf(SharpCoreDBError);
    });
  });

  describe('AuthenticationError', () => {
    it('should create authentication error', () => {
      const error = new AuthenticationError('Auth failed');
      expect(error.message).toBe('Auth failed');
      expect(error.name).toBe('AuthenticationError');
      expect(error).toBeInstanceOf(SharpCoreDBError);
    });
  });

  describe('QueryError', () => {
    it('should create query error with details', () => {
      const error = new QueryError('Query failed', 'SELECT * FROM test', { param: 1 }, 123);
      expect(error.message).toBe('Query failed');
      expect(error.name).toBe('QueryError');
      expect(error.sql).toBe('SELECT * FROM test');
      expect(error.parameters).toEqual({ param: 1 });
      expect(error.errorCode).toBe(123);
      expect(error).toBeInstanceOf(SharpCoreDBError);
    });

    it('should create query error without details', () => {
      const error = new QueryError('Query failed');
      expect(error.message).toBe('Query failed');
      expect(error.sql).toBeUndefined();
      expect(error.parameters).toBeUndefined();
      expect(error.errorCode).toBeUndefined();
    });
  });

  describe('ConfigurationError', () => {
    it('should create configuration error', () => {
      const error = new ConfigurationError('Config error');
      expect(error.message).toBe('Config error');
      expect(error.name).toBe('ConfigurationError');
      expect(error).toBeInstanceOf(SharpCoreDBError);
    });
  });
});
