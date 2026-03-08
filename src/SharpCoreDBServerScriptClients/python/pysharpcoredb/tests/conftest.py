"""Test configuration for PySharpDB."""

import pytest
import asyncio


@pytest.fixture
def event_loop():
    """Create an instance of the default event loop for the test session."""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()


@pytest.fixture
def mock_connection():
    """Mock Connection for testing."""
    from unittest.mock import Mock
    from pysharpcoredb import Connection

    conn = Mock(spec=Connection)
    conn.is_connected = False
    conn.connect = asyncio.coroutine(lambda: None)()
    conn.disconnect = asyncio.coroutine(lambda: None)()
    return conn


@pytest.fixture
def mock_pool():
    """Mock ConnectionPool for testing."""
    from unittest.mock import Mock
    from pysharpcoredb import ConnectionPool

    pool = Mock(spec=ConnectionPool)
    pool.is_closed = False
    pool.close = asyncio.coroutine(lambda: None)()
    pool.stats = {"available": 0, "in_use": 0, "total_created": 0, "total_destroyed": 0, "max_connections": 10}
    return pool
