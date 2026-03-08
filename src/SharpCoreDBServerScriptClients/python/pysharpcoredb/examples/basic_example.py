#!/usr/bin/env python3
"""Example: Basic connection and query execution with PySharpDB."""

import asyncio
import pysharpcoredb as scdb


async def main():
    """Demonstrate basic database operations."""
    print("PySharpDB Basic Example")
    print("=" * 30)

    try:
        # Connect to SharpCoreDB Server
        async with scdb.connect("grpc://localhost:5001", database="example") as conn:
            print("✅ Connected to database")

            # Create a test table
            await conn.execute_non_query("""
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    age INTEGER,
                    email TEXT
                )
            """)
            print("✅ Created users table")

            # Insert some data
            affected = await conn.execute_non_query(
                "INSERT INTO users (name, age, email) VALUES (?, ?, ?)",
                {"name": "Alice", "age": 30, "email": "alice@example.com"}
            )
            print(f"✅ Inserted {affected} row(s)")

            # Query the data
            result = await conn.execute("SELECT * FROM users WHERE age >= ?", {"age": 25})
            print(f"✅ Found {len(result)} user(s) aged 25+:")

            for row in result.rows:
                print(f"  - {row.values[1]} ({row.values[2]} years old)")

            # Ping the server
            latency = await conn.ping()
            print(f"✅ Server latency: {latency:.2f}ms")
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1

    print("\n🎉 Example completed successfully!")
    return 0


if __name__ == "__main__":
    exit_code = asyncio.run(main())
    exit(exit_code)
