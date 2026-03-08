#!/usr/bin/env python3
"""Example: Connection pooling for high-performance applications."""

import asyncio
import time
import pysharpcoredb as scdb


async def worker(worker_id: int, pool: scdb.ConnectionPool, tasks: int):
    """Worker function that uses connections from the pool."""
    results = []

    for i in range(tasks):
        async with pool.get_connection() as conn:
            # Simulate some work
            result = await conn.execute("SELECT 1 as worker, ? as task", {"task": i})
            results.append(result.rows[0].values)
            await asyncio.sleep(0.01)  # Simulate processing time

    print(f"Worker {worker_id}: Completed {tasks} tasks")
    return results


async def main():
    """Demonstrate connection pooling with concurrent workers."""
    print("PySharpDB Connection Pooling Example")
    print("=" * 40)

    # Create a connection pool
    pool_config = {
        "host": "localhost",
        "port": 5001,
        "database": "example",
        "min_connections": 2,
        "max_connections": 10,
        "max_idle_time": 60.0,
        "acquire_timeout": 10.0
    }

    try:
        async with scdb.create_pool(**pool_config) as pool:
            print("✅ Created connection pool")
            print(f"   Max connections: {pool.max_connections}")
            print(f"   Min connections: {pool.min_connections}")

            # Show initial stats
            stats = pool.stats
            print(f"   Initial stats: {stats}")

            # Run concurrent workers
            num_workers = 5
            tasks_per_worker = 10

            print(f"\n🚀 Starting {num_workers} workers with {tasks_per_worker} tasks each...")

            start_time = time.time()

            # Create worker tasks
            tasks = [
                worker(i, pool, tasks_per_worker)
                for i in range(num_workers)
            ]

            # Run all workers concurrently
            results = await asyncio.gather(*tasks)

            end_time = time.time()

            # Show final stats
            final_stats = pool.stats
            print(f"\n📊 Final pool stats: {final_stats}")

            total_tasks = sum(len(worker_results) for worker_results in results)
            total_time = end_time - start_time

            print("\n🎯 Performance Results:")
            print(f"   Total tasks completed: {total_tasks}")
            print(f"   Total time: {total_time:.2f}s")
            print(f"   Tasks/second: {total_tasks / total_time:.1f}")
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1

    print("\n🎉 Pooling example completed successfully!")
    return 0


if __name__ == "__main__":
    exit_code = asyncio.run(main())
    exit(exit_code)
