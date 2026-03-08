#!/usr/bin/env node

/**
 * Connection Pooling Example
 * Demonstrates efficient connection reuse for high-performance applications
 */

const { createPool } = require('../dist/index.js');

async function worker(workerId, pool, tasks) {
  const results = [];

  for (let i = 0; i < tasks; i++) {
    // Get connection from pool
    const connection = await pool.getConnection();

    try {
      // Simulate some work
      const result = await connection.execute('SELECT ? as worker, ? as task', {
        worker: workerId,
        task: i
      });
      results.push(result.rows[0]);

      // Simulate processing time
      await new Promise(resolve => setTimeout(resolve, 10));
    } finally {
      // Return connection to pool
      await connection.close();
    }
  }

  console.log(`👷 Worker ${workerId}: Completed ${tasks} tasks`);
  return results;
}

async function main() {
  console.log('🏊 @sharpcoredb/client Pooling Example');
  console.log('======================================');

  let pool;

  try {
    // Create a connection pool
    console.log('🏗️  Creating connection pool...');
    const poolConfig = {
      database: 'example',
      minConnections: 2,
      maxConnections: 10,
      maxIdleTime: 60000, // 1 minute
      acquireTimeout: 10000 // 10 seconds
    };

    pool = await createPool('localhost', 5001, poolConfig);
    console.log('✅ Pool created');
    console.log(`   Max connections: ${poolConfig.maxConnections}`);
    console.log(`   Min connections: ${poolConfig.minConnections}`);

    // Show initial stats
    const initialStats = pool.stats;
    console.log(`   Initial stats: ${JSON.stringify(initialStats)}`);

    // Run concurrent workers
    const numWorkers = 5;
    const tasksPerWorker = 10;
    console.log(`\n🚀 Starting ${numWorkers} workers with ${tasksPerWorker} tasks each...`);

    const startTime = Date.now();

    // Create worker tasks
    const workerPromises = [];
    for (let i = 0; i < numWorkers; i++) {
      workerPromises.push(worker(i, pool, tasksPerWorker));
    }

    // Run all workers concurrently
    const results = await Promise.all(workerPromises);

    const endTime = Date.now();
    const totalTime = endTime - startTime;
    const totalTasks = results.reduce((sum, workerResults) => sum + workerResults.length, 0);

    // Show final stats
    const finalStats = pool.stats;
    console.log(`\n📊 Final pool stats: ${JSON.stringify(finalStats)}`);

    console.log('\n🎯 Performance Results:');
    console.log(`   Total tasks completed: ${totalTasks}`);
    console.log(`   Total time: ${(totalTime / 1000).toFixed(2)}s`);
    console.log(`   Tasks/second: ${(totalTasks / (totalTime / 1000)).toFixed(1)}`);
    console.log(`   Avg latency: ~${((totalTime / totalTasks) + 10).toFixed(0)}ms per task`);

  } catch (error) {
    console.error('❌ Error:', error.message);
    process.exit(1);
  } finally {
    if (pool) {
      console.log('\n🔌 Closing pool...');
      await pool.close();
      console.log('✅ Pool closed');
    }
  }

  console.log('\n🎉 Pooling example completed successfully!');
}

// Run the example
main().catch(console.error);
