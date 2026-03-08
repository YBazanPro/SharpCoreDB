"""Tests for PySharpDB connection pooling."""

import pytest
import asyncio
from unittest.mock import Mock, AsyncMock

from pysharpcoredb import ConnectionPool, ConnectionError


class TestConnectionPool:
    """Test ConnectionPool class."""

    @pytest.mark.asyncio
    async def test_pool_initialization(self):
        """Test pool initialization."""
        pool = ConnectionPool(
            host="localhost",
            port=5001,
            database="testdb",
            min_connections=1,
            max_connections=5
        )

        assert pool.host == "localhost"
        assert pool.port == 5001
        assert pool.database == "testdb"
        assert pool.min_connections == 1
        assert pool.max_connections == 5
        assert not pool.is_closed

    @pytest.mark.asyncio
    async def test_pool_context_manager(self):
        """Test pool as async context manager."""
        pool = ConnectionPool("localhost", 5001, "testdb")
        pool.close = AsyncMock()

        async with pool:
            pass

        assert pool.close.called

    @pytest.mark.asyncio
    async def test_pool_stats(self):
        """Test pool statistics."""
        pool = ConnectionPool("localhost", 5001, "testdb")

        stats = pool.stats
        assert "available" in stats
        assert "in_use" in stats
        assert "total_created" in stats
        assert "total_destroyed" in stats
        assert stats["max_connections"] == 10  # default

    @pytest.mark.asyncio
    async def test_pool_closed_operations(self):
        """Test operations on closed pool."""
        pool = ConnectionPool("localhost", 5001, "testdb")
        await pool.close()

        assert pool.is_closed

        with pytest.raises(ConnectionError):
            await pool.get_connection()

    def test_create_pool_context_manager(self):
        """Test create_pool context manager function."""
        from pysharpcoredb import create_pool
        assert callable(create_pool)
