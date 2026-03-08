"""Connection management for PySharpDB."""

import asyncio
import logging
from contextlib import asynccontextmanager
from typing import Optional, Union
from urllib.parse import urlparse

from .exceptions import ConnectionError, ConfigurationError
from .grpc_client import GrpcClient
from .http_client import HttpClient
from .types import ConnectionInfo, ResultSet
from .ws_client import WebSocketClient

logger = logging.getLogger(__name__)


class Connection:
    """A connection to a SharpCoreDB Server.

    Supports automatic protocol selection (gRPC preferred, falls back to HTTP/WebSocket).
    """

    def __init__(
        self,
        host: str,
        port: Optional[int] = None,
        database: str = "default",
        username: Optional[str] = None,
        password: Optional[str] = None,
        tls: bool = True,
        timeout: float = 30.0,
        **kwargs
    ):
        """Initialize a connection.

        Args:
            host: Server hostname or IP
            port: Server port (default: 5001 for gRPC, 8443 for HTTP)
            database: Database name to connect to
            username: Username for authentication
            password: Password for authentication
            tls: Whether to use TLS (default: True)
            timeout: Connection timeout in seconds
        """
        self.host = host
        self.port = port
        self.database = database
        self.username = username
        self.password = password
        self.tls = tls
        self.timeout = timeout

        self._connection_info: Optional[ConnectionInfo] = None
        self._connected = False
        self._grpc_client: Optional[GrpcClient] = None
        self._http_client: Optional[HttpClient] = None
        self._ws_client: Optional[WebSocketClient] = None

        # Protocol preference: gRPC -> HTTP -> WebSocket
        self._preferred_protocols = ["grpc", "http", "websocket"]

    async def connect(self) -> None:
        """Establish connection to the server."""
        if self._connected:
            return

        # Try protocols in order of preference
        for protocol in self._preferred_protocols:
            try:
                if protocol == "grpc":
                    await self._connect_grpc()
                elif protocol == "http":
                    await self._connect_http()
                elif protocol == "websocket":
                    await self._connect_websocket()

                self._connected = True
                logger.info(f"Connected to SharpCoreDB Server at {self.host}:{self.port} using {protocol}")
                return
            except Exception as e:
                logger.debug(f"Failed to connect using {protocol}: {e}")
                continue

        raise ConnectionError(f"Failed to connect to SharpCoreDB Server at {self.host}:{self.port}")

    async def _connect_grpc(self) -> None:
        """Connect using gRPC protocol."""
        port = self.port or 5001
        self._grpc_client = GrpcClient(
            host=self.host,
            port=port,
            tls=self.tls,
            timeout=self.timeout
        )

        self._connection_info = await self._grpc_client.connect(
            database=self.database,
            username=self.username,
            password=self.password
        )

    async def _connect_http(self) -> None:
        """Connect using HTTP REST API."""
        port = self.port or 8443
        self._http_client = HttpClient(
            host=self.host,
            port=port,
            tls=self.tls,
            timeout=self.timeout
        )

        self._connection_info = await self._http_client.connect(
            database=self.database,
            username=self.username,
            password=self.password
        )

    async def _connect_websocket(self) -> None:
        """Connect using WebSocket protocol."""
        port = self.port or 8443  # WebSocket typically on same port as HTTPS
        self._ws_client = WebSocketClient(
            host=self.host,
            port=port,
            tls=self.tls,
            timeout=self.timeout
        )

        self._connection_info = await self._ws_client.connect(
            database=self.database,
            username=self.username,
            password=self.password
        )

    async def disconnect(self) -> None:
        """Close the connection."""
        if not self._connected:
            return

        # Close all clients
        if self._grpc_client:
            await self._grpc_client.disconnect()
        if self._http_client:
            await self._http_client.disconnect()
        if self._ws_client:
            await self._ws_client.disconnect()

        self._connected = False
        logger.info("Disconnected from SharpCoreDB Server")

    async def execute(self, sql: str, parameters: Optional[dict] = None) -> ResultSet:
        """Execute a SQL query.

        Args:
            sql: SQL query string
            parameters: Query parameters

        Returns:
            ResultSet containing the query results
        """
        if not self._connected:
            raise ConnectionError("Not connected to server")

        if self._grpc_client:
            return await self._grpc_client.execute_query(sql, parameters)
        elif self._http_client:
            return await self._http_client.execute_query(sql, parameters)
        elif self._ws_client:
            return await self._ws_client.execute_query(sql, parameters)

        raise NotImplementedError("Query execution not implemented for current protocol")

    async def execute_non_query(self, sql: str, parameters: Optional[dict] = None) -> int:
        """Execute a non-query SQL statement.

        Args:
            sql: SQL statement
            parameters: Statement parameters

        Returns:
            Number of affected rows
        """
        if not self._connected:
            raise ConnectionError("Not connected to server")

        if self._grpc_client:
            return await self._grpc_client.execute_non_query(sql, parameters)
        elif self._http_client:
            return await self._http_client.execute_non_query(sql, parameters)
        elif self._ws_client:
            return await self._ws_client.execute_non_query(sql, parameters)

        raise NotImplementedError("Non-query execution not implemented for current protocol")

    async def ping(self) -> float:
        """Ping the server and return round-trip time in milliseconds."""
        if not self._connected:
            raise ConnectionError("Not connected to server")

        if self._grpc_client:
            return await self._grpc_client.ping()
        elif self._http_client:
            return await self._http_client.ping()
        elif self._ws_client:
            return await self._ws_client.ping()

        raise NotImplementedError("Ping not implemented for current protocol")

    @property
    def is_connected(self) -> bool:
        """Check if the connection is active."""
        return self._connected

    @property
    def connection_info(self) -> Optional[ConnectionInfo]:
        """Get connection information."""
        return self._connection_info

    async def __aenter__(self):
        await self.connect()
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.disconnect()


@asynccontextmanager
async def connect(
    url: str,
    database: str = "default",
    username: Optional[str] = None,
    password: Optional[str] = None,
    **kwargs
):
    """Connect to a SharpCoreDB Server.

    Args:
        url: Connection URL (e.g., "grpc://localhost:5001", "https://localhost:8443")
        database: Database name
        username: Username
        password: Password
        **kwargs: Additional connection options

    Yields:
        Connection instance
    """
    # Parse URL
    parsed = urlparse(url)
    if not parsed.hostname:
        raise ConfigurationError(f"Invalid URL: {url}")

    # Determine protocol and port
    protocol = parsed.scheme
    port = parsed.port

    if protocol == "grpc":
        port = port or 5001
    elif protocol in ("http", "https"):
        port = port or (443 if parsed.scheme == "https" else 80)
    elif protocol == "ws" or protocol == "wss":
        port = port or (443 if parsed.scheme == "wss" else 80)
    else:
        raise ConfigurationError(f"Unsupported protocol: {protocol}")

    conn = Connection(
        host=parsed.hostname,
        port=port,
        database=database,
        username=username,
        password=password,
        tls=protocol in ("https", "grpc", "wss"),
        **kwargs
    )

    try:
        async with conn:
            yield conn
    finally:
        pass
