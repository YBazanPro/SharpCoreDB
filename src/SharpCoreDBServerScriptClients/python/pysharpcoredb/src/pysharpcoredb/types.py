"""Type definitions for PySharpDB."""

from typing import Any, Dict, List, Optional, Union
from dataclasses import dataclass
from datetime import datetime


@dataclass
class Column:
    """Represents a column in a result set."""
    name: str
    type_name: str
    nullable: bool = True
    max_length: Optional[int] = None


@dataclass
class Row:
    """Represents a single row in a result set."""
    values: List[Any]

    def __getitem__(self, key: Union[str, int]) -> Any:
        if isinstance(key, str):
            # TODO: Implement column name lookup
            raise NotImplementedError("Column name lookup not yet implemented")
        return self.values[key]

    def __len__(self) -> int:
        return len(self.values)


@dataclass
class ResultSet:
    """Represents the result of a query execution."""
    columns: List[Column]
    rows: List[Row]
    row_count: int
    execution_time_ms: float
    has_more: bool = False

    def __len__(self) -> int:
        return len(self.rows)

    def __getitem__(self, index: int) -> Row:
        return self.rows[index]

    def __iter__(self):
        return iter(self.rows)


@dataclass
class ConnectionInfo:
    """Information about a database connection."""
    database_name: str
    session_id: Optional[str] = None
    server_version: Optional[str] = None
    connected_at: Optional[datetime] = None


@dataclass
class ServerInfo:
    """Information about the SharpCoreDB Server."""
    version: str
    uptime_seconds: int
    active_connections: int
    supported_protocols: List[str]


# Type aliases
ParameterValue = Union[
    None, bool, int, float, str, bytes, datetime,
    List[float],  # For vectors
    Dict[str, Any]  # For complex types
]

QueryParameters = Dict[str, ParameterValue]
