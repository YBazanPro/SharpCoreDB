/**
 * @jest-environment node
 */

import { ConnectionPool } from '../src/pool';
import { ConnectionError } from '../src/errors';

describe('ConnectionPool', () => {
  describe('initialization', () => {
    it('should create pool with correct parameters', () => {
      const pool = new ConnectionPool('localhost', 5001, {
        database: 'testdb',
        minConnections: 1,
        maxConnections: 5,
        maxIdleTime: 60000
      });

      expect(pool).toBeDefined();
      expect(pool.host).toBe('localhost');
      expect(pool.port).toBe(5001);
      expect(pool.database).toBe('testdb');
      expect(pool.isClosed).toBe(false);
    });

    it('should have default values', () => {
      const pool = new ConnectionPool('localhost', 5001);
      expect(pool).toBeDefined();
      expect(pool.database).toBe('default');
    });
  });

  describe('pool lifecycle', () => {
    it('should allow closing pool', async () => {
      const pool = new ConnectionPool('localhost', 5001);
      await expect(pool.close()).resolves.toBeUndefined();
      expect(pool.isClosed).toBe(true);
    });

    it('should reject operations on closed pool', async () => {
      const pool = new ConnectionPool('localhost', 5001);
      await pool.close();

      await expect(pool.getConnection()).rejects.toThrow(ConnectionError);
    });
  });

  describe('statistics', () => {
    it('should provide pool statistics', () => {
      const pool = new ConnectionPool('localhost', 5001, {
        maxConnections: 10
      });

      const stats = pool.stats;
      expect(stats).toHaveProperty('available');
      expect(stats).toHaveProperty('inUse');
      expect(stats).toHaveProperty('totalCreated');
      expect(stats).toHaveProperty('totalDestroyed');
      expect(stats.maxConnections).toBe(10);
    });
  });
});
