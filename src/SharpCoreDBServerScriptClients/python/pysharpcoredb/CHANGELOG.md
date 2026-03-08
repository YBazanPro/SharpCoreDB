# PySharpDB Changelog

All notable changes to PySharpDB will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-01-28

### Added
- Initial release of PySharpDB
- Support for gRPC, HTTP REST, and WebSocket protocols
- Automatic protocol selection with fallback
- Connection pooling for high-performance applications
- Async/await support with asyncio
- Comprehensive error handling and exceptions
- Type hints and modern Python patterns
- Full test suite with pytest
- Documentation and examples

### Features
- **Multi-Protocol Support**: Connect via gRPC (recommended), HTTP, or WebSocket
- **Connection Pooling**: Efficient connection reuse with configurable limits
- **Async Operations**: Full asyncio support for concurrent database operations
- **Automatic Failover**: Seamless protocol fallback if primary protocol unavailable
- **Type Safety**: Complete type hints for better IDE support
- **Error Handling**: Specific exception types for different error conditions
- **Performance Monitoring**: Built-in connection statistics and latency tracking

### Examples
```python
import pysharpcoredb as scdb

# Simple connection
async with scdb.connect("grpc://localhost:5001") as conn:
    result = await conn.execute("SELECT * FROM users")

# Connection pooling
async with scdb.create_pool(host="localhost", max_connections=20) as pool:
    async with pool.get_connection() as conn:
        await conn.execute_non_query("INSERT INTO users VALUES (?)", {"name": "Alice"})
```

### Dependencies
- grpcio>=1.50.0
- requests>=2.28.0
- websockets>=11.0.0
- pydantic>=2.0.0
- typing-extensions>=4.5.0

## [Unreleased]

### Planned
- Streaming query results for large datasets
- Prepared statement support
- Transaction management
- Connection retry logic with exponential backoff
- Metrics collection and OpenTelemetry integration
- Python 3.12+ optimizations
- Binary protocol support
- Authentication token refresh
- Connection health monitoring
- Bulk operation helpers
