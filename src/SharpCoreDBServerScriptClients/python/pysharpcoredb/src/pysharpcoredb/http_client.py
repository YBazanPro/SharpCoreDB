"""HTTP REST client implementation for PySharpDB."""

import asyncio
import json
import logging
from typing import Any, Dict, List, Optional

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

from .exceptions import ConnectionError, AuthenticationError, QueryError
from .types import ConnectionInfo, ResultSet, Column, Row, ParameterValue

logger = logging.getLogger(__name__)


class HttpClient:
    """HTTP REST client for SharpCoreDB Server."""

    def __init__(self, host: str, port: int, tls: bool = True, timeout: float = 30.0):
        self.host = host
        self.port = port
        self.tls = tls
        self.timeout = timeout

        self._base_url = f"{'https' if tls else 'http'}://{host}:{port}/api"
        self._session: Optional[requests.Session] = None
        self._auth_token: Optional[str] = None
        self._database: Optional[str] = None

        # Configure session with retries
        self._session = requests.Session()
        retry_strategy = Retry(
            total=3,
            backoff_factor=0.3,
            status_forcelist=[429, 500, 502, 503, 504],
        )
        adapter = HTTPAdapter(max_retries=retry_strategy)
        self._session.mount("http://", adapter)
        self._session.mount("https://", adapter)

    async def connect(self, database: str = "default", username: Optional[str] = None,
                     password: Optional[str] = None) -> ConnectionInfo:
        """Establish HTTP connection and authenticate."""
        self._database = database

        # For HTTP, authentication is done per request
        # We'll store credentials for future requests
        if username and password:
            # TODO: Implement JWT authentication
            # For now, we'll use basic auth or API key
            pass

        # Test connection with a ping/health check
        try:
            response = self._session.get(
                f"{'https' if self.tls else 'http'}://{self.host}:{self.port}/health",
                timeout=self.timeout
            )
            response.raise_for_status()

            health_data = response.json()
            server_version = health_data.get("version", "unknown")

            return ConnectionInfo(
                database_name=database,
                server_version=server_version
            )

        except requests.RequestException as e:
            raise ConnectionError(f"Failed to connect to HTTP endpoint: {e}")

    async def disconnect(self) -> None:
        """Close the HTTP connection."""
        if self._session:
            self._session.close()
            self._session = None
        self._auth_token = None
        self._database = None

    async def execute_query(self, sql: str, parameters: Optional[Dict[str, ParameterValue]] = None) -> ResultSet:
        """Execute a SELECT query via HTTP."""
        if not self._session or not self._database:
            raise ConnectionError("Not connected")

        url = f"{self._base_url}/query"
        payload = {
            "sql": sql,
            "database": self._database
        }

        if parameters:
            payload["parameters"] = parameters

        try:
            response = self._session.post(
                url,
                json=payload,
                headers=self._get_headers(),
                timeout=self.timeout
            )
            response.raise_for_status()

            data = response.json()

            # Parse response
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

        except requests.HTTPError as e:
            if e.response.status_code == 401:
                raise AuthenticationError(f"Authentication failed: {e.response.text}")
            elif e.response.status_code == 400:
                raise QueryError(f"Query error: {e.response.text}", sql=sql)
            else:
                raise QueryError(f"HTTP error {e.response.status_code}: {e.response.text}", sql=sql)
        except requests.RequestException as e:
            raise ConnectionError(f"HTTP request failed: {e}")

    async def execute_non_query(self, sql: str, parameters: Optional[Dict[str, ParameterValue]] = None) -> int:
        """Execute INSERT/UPDATE/DELETE via HTTP."""
        if not self._session or not self._database:
            raise ConnectionError("Not connected")

        url = f"{self._base_url}/nonquery"
        payload = {
            "sql": sql,
            "database": self._database
        }

        if parameters:
            payload["parameters"] = parameters

        try:
            response = self._session.post(
                url,
                json=payload,
                headers=self._get_headers(),
                timeout=self.timeout
            )
            response.raise_for_status()

            data = response.json()
            return data.get("rowsAffected", 0)

        except requests.HTTPError as e:
            if e.response.status_code == 401:
                raise AuthenticationError(f"Authentication failed: {e.response.text}")
            elif e.response.status_code == 400:
                raise QueryError(f"Query error: {e.response.text}", sql=sql)
            else:
                raise QueryError(f"HTTP error {e.response.status_code}: {e.response.text}", sql=sql)
        except requests.RequestException as e:
            raise ConnectionError(f"HTTP request failed: {e}")

    async def ping(self) -> float:
        """Ping the server via HTTP."""
        if not self._session:
            raise ConnectionError("Not connected")

        import time
        start_time = time.time()

        try:
            response = self._session.get(
                f"{'https' if self.tls else 'http'}://{self.host}:{self.port}/health",
                timeout=self.timeout
            )
            response.raise_for_status()

            end_time = time.time()
            return (end_time - start_time) * 1000

        except requests.RequestException as e:
            raise ConnectionError(f"Ping failed: {e}")

    def _get_headers(self) -> Dict[str, str]:
        """Get HTTP headers for requests."""
        headers = {
            "Content-Type": "application/json",
            "User-Agent": "PySharpDB/1.5.0"
        }

        if self._auth_token:
            headers["Authorization"] = f"Bearer {self._auth_token}"

        return headers

    @property
    def is_connected(self) -> bool:
        """Check if connected."""
        return self._session is not None and self._database is not None
