"""WebSocket client implementation for PySharpDB."""

import asyncio
import json
import logging
from typing import Any, Dict, List, Optional

import websockets
from websockets.exceptions import ConnectionClosedError, WebSocketException

from .exceptions import ConnectionError, AuthenticationError, QueryError
from .types import ConnectionInfo, ResultSet, Column, Row, ParameterValue

logger = logging.getLogger(__name__)


class WebSocketClient:
    """WebSocket client for SharpCoreDB Server streaming operations."""

    def __init__(self, host: str, port: int, tls: bool = True, timeout: float = 30.0):
        self.host = host
        self.port = port
        self.tls = tls
        self.timeout = timeout

        self._websocket: Optional[websockets.WebSocketServerProtocol] = None
        self._session_id: Optional[str] = None
        self._database: Optional[str] = None
        self._message_id = 0

        # Response queues for async operations
        self._response_queues: Dict[int, asyncio.Queue] = {}

    async def connect(self, database: str = "default", username: Optional[str] = None,
                     password: Optional[str] = None) -> ConnectionInfo:
        """Establish WebSocket connection and authenticate."""
        self._database = database

        uri = f"{'wss' if self.tls else 'ws'}://{self.host}:{self.port}/ws"

        try:
            self._websocket = await websockets.connect(
                uri,
                extra_headers=self._get_headers(username, password),
                open_timeout=self.timeout
            )

            # Start message handler
            asyncio.create_task(self._message_handler())

            # TODO: Send authentication message if needed
            # For now, assume connection is authenticated

            return ConnectionInfo(
                database_name=database,
                server_version="1.5.0"  # TODO: Get from server
            )

        except (WebSocketException, asyncio.TimeoutError) as e:
            raise ConnectionError(f"Failed to connect to WebSocket: {e}")

    async def disconnect(self) -> None:
        """Close the WebSocket connection."""
        if self._websocket:
            await self._websocket.close()
            self._websocket = None
        self._session_id = None
        self._database = None
        self._response_queues.clear()

    async def execute_query(self, sql: str, parameters: Optional[Dict[str, ParameterValue]] = None) -> ResultSet:
        """Execute a SELECT query via WebSocket."""
        if not self._websocket or not self._database:
            raise ConnectionError("Not connected")

        message_id = self._get_next_message_id()
        queue = asyncio.Queue()
        self._response_queues[message_id] = queue

        message = {
            "id": message_id,
            "type": "query",
            "sql": sql,
            "database": self._database
        }

        if parameters:
            message["parameters"] = parameters

        try:
            await self._websocket.send(json.dumps(message))

            # Wait for response
            response = await asyncio.wait_for(
                queue.get(),
                timeout=self.timeout
            )

            if response.get("error"):
                raise QueryError(response["error"], sql=sql)

            # Parse response
            data = response.get("data", {})
            columns = [
                Column(name=col["name"], type_name=col["type"], nullable=col.get("nullable", True))
                for col in data.get("columns", [])
            ]
            rows = [Row(values=row) for row in data.get("rows", [])]

            return ResultSet(
                columns=columns,
                rows=rows,
                row_count=len(rows),
                execution_time_ms=data.get("executionTimeMs", 0.0)
            )

        except asyncio.TimeoutError:
            raise QueryError("Query timeout", sql=sql)
        finally:
            self._response_queues.pop(message_id, None)

    async def execute_non_query(self, sql: str, parameters: Optional[Dict[str, ParameterValue]] = None) -> int:
        """Execute INSERT/UPDATE/DELETE via WebSocket."""
        if not self._websocket or not self._database:
            raise ConnectionError("Not connected")

        message_id = self._get_next_message_id()
        queue = asyncio.Queue()
        self._response_queues[message_id] = queue

        message = {
            "id": message_id,
            "type": "nonquery",
            "sql": sql,
            "database": self._database
        }

        if parameters:
            message["parameters"] = parameters

        try:
            await self._websocket.send(json.dumps(message))

            # Wait for response
            response = await asyncio.wait_for(
                queue.get(),
                timeout=self.timeout
            )

            if response.get("error"):
                raise QueryError(response["error"], sql=sql)

            return response.get("data", {}).get("rowsAffected", 0)

        except asyncio.TimeoutError:
            raise QueryError("Non-query timeout", sql=sql)
        finally:
            self._response_queues.pop(message_id, None)

    async def ping(self) -> float:
        """Ping the server via WebSocket."""
        if not self._websocket:
            raise ConnectionError("Not connected")

        import time
        start_time = time.time()

        message_id = self._get_next_message_id()
        queue = asyncio.Queue()
        self._response_queues[message_id] = queue

        try:
            await self._websocket.send(json.dumps({
                "id": message_id,
                "type": "ping"
            }))

            # Wait for pong response
            response = await asyncio.wait_for(
                queue.get(),
                timeout=self.timeout
            )

            end_time = time.time()
            return (end_time - start_time) * 1000

        except asyncio.TimeoutError:
            raise ConnectionError("Ping timeout")
        finally:
            self._response_queues.pop(message_id, None)

    async def _message_handler(self) -> None:
        """Handle incoming WebSocket messages."""
        try:
            while self._websocket:
                try:
                    message = await self._websocket.recv()
                    data = json.loads(message)

                    message_id = data.get("id")
                    if message_id in self._response_queues:
                        await self._response_queues[message_id].put(data)

                except ConnectionClosedError:
                    logger.info("WebSocket connection closed")
                    break
                except json.JSONDecodeError as e:
                    logger.error(f"Invalid JSON message: {e}")
                    continue

        except Exception as e:
            logger.error(f"Message handler error: {e}")

    def _get_next_message_id(self) -> int:
        """Get next message ID."""
        self._message_id += 1
        return self._message_id

    def _get_headers(self, username: Optional[str], password: Optional[str]) -> Dict[str, str]:
        """Get WebSocket headers for authentication."""
        headers = {
            "User-Agent": "PySharpDB/1.5.0"
        }

        # TODO: Add authentication headers if needed
        if username and password:
            # Could add basic auth or JWT
            pass

        return headers

    @property
    def is_connected(self) -> bool:
        """Check if connected."""
        return self._websocket is not None and not self._websocket.closed
