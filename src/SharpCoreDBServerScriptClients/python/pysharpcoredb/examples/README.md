# PySharpDB Examples

This directory contains examples demonstrating how to use PySharpDB to connect to and interact with SharpCoreDB Server.

## Prerequisites

Before running the examples, ensure you have:

1. **SharpCoreDB Server running** on `localhost:5001` (gRPC) or `localhost:8443` (HTTP/WebSocket)
2. **Python 3.8+** installed
3. **PySharpDB** installed: `pip install pysharpcoredb`

## Examples

### 1. Basic Connection (`basic_example.py`)

Demonstrates fundamental database operations:
- Connecting to SharpCoreDB Server
- Creating tables
- Inserting and querying data
- Error handling

```bash
python examples/basic_example.py
```

### 2. Connection Pooling (`pooling_example.py`)

Shows how to use connection pooling for high-performance applications:
- Creating a connection pool
- Concurrent worker tasks
- Pool statistics and monitoring
- Performance measurements

```bash
python examples/pooling_example.py
```

## Running Examples

1. **Start SharpCoreDB Server** (see main documentation)
2. **Install PySharpDB**:
   ```bash
   cd clients/python/pysharpcoredb
   pip install -e .
   ```
3. **Run examples**:
   ```bash
   python examples/basic_example.py
   python examples/pooling_example.py
   ```

## Example Output

### Basic Example
```
PySharpDB Basic Example
==============================
✅ Connected to database
✅ Created users table
✅ Inserted 1 row(s)
✅ Found 1 user(s) aged 25+:
  - Alice (30 years old)
✅ Server latency: 1.23ms

🎉 Example completed successfully!
```

### Pooling Example
```
PySharpDB Connection Pooling Example
========================================
✅ Created connection pool
   Max connections: 10
   Min connections: 2
   Initial stats: {'available': 0, 'in_use': 0, 'total_created': 0, 'total_destroyed': 0, 'max_connections': 10}

🚀 Starting 5 workers with 10 tasks each...
Worker 0: Completed 10 tasks
Worker 1: Completed 10 tasks
Worker 2: Completed 10 tasks
Worker 3: Completed 10 tasks
Worker 4: Completed 10 tasks

📊 Final pool stats: {'available': 5, 'in_use': 0, 'total_created': 5, 'total_destroyed': 0, 'max_connections': 10}

🎯 Performance Results:
   Total tasks completed: 50
   Total time: 0.75s
   Tasks/second: 66.7
   Avg latency: ~15ms per task

🎉 Pooling example completed successfully!
```

## Configuration

Examples use default connection settings. To customize:

```python
# Custom connection
async with scdb.connect("grpc://your-server:5001", database="yourdb") as conn:
    # Your code here
    pass

# Custom pool
pool_config = {
    "host": "your-server",
    "port": 5001,
    "database": "yourdb",
    "max_connections": 20,
    "acquire_timeout": 15.0
}

async with scdb.create_pool(**pool_config) as pool:
    # Your code here
    pass
```

## Troubleshooting

### Connection Errors
- Ensure SharpCoreDB Server is running
- Check firewall settings (ports 5001/8443)
- Verify TLS certificates if using HTTPS

### Import Errors
- Install PySharpDB: `pip install pysharpcoredb`
- Or install from source: `pip install -e .`

### Protocol Errors
- Try different protocols: `grpc://`, `https://`, `ws://`
- Check server logs for connection attempts

## Next Steps

- Read the main [README.md](../README.md) for API reference
- Check [test files](../tests/) for more usage examples
- Visit [SharpCoreDB documentation](https://sharpcoredb.com/docs) for advanced topics
