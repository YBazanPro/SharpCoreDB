"""Exception classes for PySharpDB."""


class SharpCoreDBError(Exception):
    """Base exception for all PySharpDB errors."""
    pass


class ConnectionError(SharpCoreDBError):
    """Raised when connection to SharpCoreDB Server fails."""
    pass


class AuthenticationError(SharpCoreDBError):
    """Raised when authentication fails."""
    pass


class QueryError(SharpCoreDBError):
    """Raised when a query execution fails."""

    def __init__(self, message: str, sql: str = None, error_code: int = None):
        super().__init__(message)
        self.sql = sql
        self.error_code = error_code


class ConfigurationError(SharpCoreDBError):
    """Raised when client configuration is invalid."""
    pass


class TimeoutError(SharpCoreDBError):
    """Raised when an operation times out."""
    pass
