"""Tests for PySharpDB exceptions."""

import pytest

from pysharpcoredb import (
    SharpCoreDBError,
    ConnectionError,
    AuthenticationError,
    QueryError,
    ConfigurationError,
    TimeoutError
)


class TestExceptions:
    """Test exception classes."""

    def test_base_exception(self):
        """Test base SharpCoreDBError."""
        error = SharpCoreDBError("Test error")
        assert str(error) == "Test error"
        assert isinstance(error, Exception)

    def test_connection_error(self):
        """Test ConnectionError."""
        error = ConnectionError("Connection failed")
        assert str(error) == "Connection failed"
        assert isinstance(error, SharpCoreDBError)

    def test_authentication_error(self):
        """Test AuthenticationError."""
        error = AuthenticationError("Auth failed")
        assert str(error) == "Auth failed"
        assert isinstance(error, SharpCoreDBError)

    def test_query_error(self):
        """Test QueryError."""
        error = QueryError("Query failed", "SELECT * FROM test")
        assert str(error) == "Query failed"
        assert error.sql == "SELECT * FROM test"
        assert error.error_code is None
        assert isinstance(error, SharpCoreDBError)

    def test_query_error_with_code(self):
        """Test QueryError with error code."""
        error = QueryError("Query failed", "SELECT * FROM test", 123)
        assert error.error_code == 123

    def test_configuration_error(self):
        """Test ConfigurationError."""
        error = ConfigurationError("Config error")
        assert str(error) == "Config error"
        assert isinstance(error, SharpCoreDBError)

    def test_timeout_error(self):
        """Test TimeoutError."""
        error = TimeoutError("Operation timed out")
        assert str(error) == "Operation timed out"
        assert isinstance(error, SharpCoreDBError)
