"""Connection pooling for PySharpDB."""

import asyncio
import logging
import time
from typing import Dict, List, Optional
from contextlib import asynccontextmanager

from .connection import Connection
from .exceptions import ConnectionError

logger = logging.getLogger(__name__)


class PooledConnection:
    """A pooled database connection."""

    def __init__(self, connection: Connection, pool: 'ConnectionPool'):
        self.connection = connection
        self.pool = pool
        self.created_at = time.time()
        self.last_used = time.time()
        self._closed = False

    async def close(self):
        """Return connection to pool."""
        if not self._closed:
            self._closed = False
            await self.pool._return_connection(self)

    async def __aenter__(self):
        return self.connection

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.close()

    @property
    def is_closed(self) -> bool:
        return self._closed


class ConnectionPool:
    """Connection pool for PySharpDB.

    Provides efficient connection reuse and management for high-concurrency applications.
    """

    def __init__(
        self,
        host: str,
        port: Optional[int] = None,
        database: str = "default",
        username: Optional[str] = None,
        password: Optional[str] = None,
        tls: bool = True,
        min_connections: int = 1,
        max_connections: int = 10,
        max_idle_time: float = 300.0,  # 5 minutes
        max_lifetime: float = 3600.0,  # 1 hour
        acquire_timeout: float = 30.0
    ):
        """Initialize the connection pool.

        Args:
            host: Server hostname
            port: Server port
            database: Database name
            username: Username
            password: Password
            tls: Use TLS
            min_connections: Minimum connections to maintain
            max_connections: Maximum connections allowed
            max_idle_time: Max idle time before closing connection
            max_lifetime: Max lifetime before recycling connection
            acquire_timeout: Timeout for acquiring connection
        """
        self.host = host
        self.port = port
        self.database = database
        self.username = username
        self.password = password
        self.tls = tls

        self.min_connections = min_connections
        self.max_connections = max_connections
        self.max_idle_time = max_idle_time
        self.max_lifetime = max_lifetime
        self.acquire_timeout = acquire_timeout

        self._available: List[PooledConnection] = []
        self._in_use: Dict[Connection, PooledConnection] = {}
        self._lock = asyncio.Lock()
        self._closed = False

        # Statistics
        self._created_count = 0
        self._destroyed_count = 0

    async def get_connection(self) -> PooledConnection:
        """Get a connection from the pool."""
        if self._closed:
            raise ConnectionError("Connection pool is closed")

        async with self._lock:
            # Try to get an available connection
            while self._available:
                pooled_conn = self._available.pop()
                if self._is_connection_valid(pooled_conn):
                    pooled_conn.last_used = time.time()
                    self._in_use[pooled_conn.connection] = pooled_conn
                    return pooled_conn
                else:
                    await self._destroy_connection(pooled_conn)

            # Create new connection if under limit
            if len(self._in_use) < self.max_connections:
                connection = Connection(
                    host=self.host,
                    port=self.port,
                    database=self.database,
                    username=self.username,
                    password=self.password,
                    tls=self.tls
                )
                await connection.connect()

                pooled_conn = PooledConnection(connection, self)
                self._created_count += 1
                self._in_use[connection] = pooled_conn
                return pooled_conn

            # Wait for a connection to become available
            # For simplicity, we'll just raise an error if at max capacity
            raise ConnectionError(
                f"Connection pool exhausted: {len(self._in_use)}/{self.max_connections} connections in use"
            )

    async def _return_connection(self, pooled_conn: PooledConnection):
        """Return a connection to the pool."""
        async with self._lock:
            if pooled_conn.connection in self._in_use:
                del self._in_use[pooled_conn.connection]

            if self._is_connection_valid(pooled_conn):
                self._available.append(pooled_conn)
            else:
                await self._destroy_connection(pooled_conn)

    async def _destroy_connection(self, pooled_conn: PooledConnection):
        """Destroy a connection."""
        try:
            await pooled_conn.connection.disconnect()
        except Exception as e:
            logger.warning(f"Error closing connection: {e}")

        self._destroyed_count += 1

    def _is_connection_valid(self, pooled_conn: PooledConnection) -> bool:
        """Check if a connection is still valid."""
        now = time.time()

        # Check lifetime
        if now - pooled_conn.created_at > self.max_lifetime:
            return False

        # Check idle time
        if now - pooled_conn.last_used > self.max_idle_time:
            return False

        # Check if connection is still connected
        return pooled_conn.connection.is_connected

    async def close(self):
        """Close the connection pool and destroy all connections."""
        if self._closed:
            return

        async with self._lock:
            self._closed = True

            # Close all available connections
            destroy_tasks = []
            for pooled_conn in self._available:
                destroy_tasks.append(self._destroy_connection(pooled_conn))

            # Close all in-use connections
            for pooled_conn in self._in_use.values():
                destroy_tasks.append(self._destroy_connection(pooled_conn))

            self._available.clear()
            self._in_use.clear()

            await asyncio.gather(*destroy_tasks, return_exceptions=True)

    @property
    def is_closed(self) -> bool:
        """Check if the pool is closed."""
        return self._closed

    @property
    def stats(self) -> Dict[str, int]:
        """Get pool statistics."""
        return {
            "available": len(self._available),
            "in_use": len(self._in_use),
            "total_created": self._created_count,
            "total_destroyed": self._destroyed_count,
            "max_connections": self.max_connections
        }

    async def __aenter__(self):
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.close()


@asynccontextmanager
async def create_pool(**kwargs):
    """Create a connection pool.

    Args:
        **kwargs: Connection pool parameters

    Yields:
        ConnectionPool instance
    """
    pool = ConnectionPool(**kwargs)
    try:
        yield pool
    finally:
        await pool.close()
