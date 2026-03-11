#nullable enable

using Dotmim.Sync;

namespace SharpCoreDB.Provider.Sync.Adapters;

/// <summary>
/// SQL command text templates for all sync operations.
/// Generates parameterized SELECT, INSERT, UPDATE, DELETE commands for sync DML.
/// Uses SharpCoreDB's SQLite-compatible SQL dialect.
/// </summary>
public sealed class SharpCoreDBObjectNames(SyncTable tableDescription)
{
    private readonly SyncTable _table = tableDescription ?? throw new ArgumentNullException(nameof(tableDescription));

    private string TableName => _table.TableName;
    private string TrackingTableName => $"{TableName}_tracking";

    private string PrimaryKeyColumn =>
        _table.PrimaryKeys.FirstOrDefault()
            ?? throw new InvalidOperationException($"Table '{TableName}' must have a primary key for sync.");

    private string AllColumns =>
        string.Join(", ", _table.Columns.Select(c => $"[{c.ColumnName}]"));

    private string AllParameters =>
        string.Join(", ", _table.Columns.Select(c => $"@{c.ColumnName}"));

    private string NonPkSetClause =>
        string.Join(", ", _table.Columns
            .Where(c => c.ColumnName != PrimaryKeyColumn)
            .Select(c => $"[{c.ColumnName}] = @{c.ColumnName}"));

    /// <summary>
    /// SELECT changed rows since a given sync timestamp.
    /// Joins the data table with its tracking table on primary key.
    /// </summary>
    public string SelectChangesCommand =>
        $"""
        SELECT {AllColumns},
               tt.[update_scope_id], tt.[timestamp], tt.[sync_row_is_tombstone], tt.[last_change_datetime]
        FROM [{TableName}] t
        INNER JOIN [{TrackingTableName}] tt ON t.[{PrimaryKeyColumn}] = tt.[{PrimaryKeyColumn}]
        WHERE tt.[timestamp] > @sync_min_timestamp
        ORDER BY tt.[timestamp]
        """;

    /// <summary>
    /// SELECT a single row by primary key.
    /// </summary>
    public string SelectRowCommand =>
        $"SELECT {AllColumns} FROM [{TableName}] WHERE [{PrimaryKeyColumn}] = @{PrimaryKeyColumn}";

    /// <summary>
    /// INSERT a new row (or replace if it already exists).
    /// </summary>
    public string InsertCommand =>
        $"INSERT OR REPLACE INTO [{TableName}] ({AllColumns}) VALUES ({AllParameters})";

    /// <summary>
    /// UPDATE an existing row by primary key.
    /// </summary>
    public string UpdateCommand =>
        $"UPDATE [{TableName}] SET {NonPkSetClause} WHERE [{PrimaryKeyColumn}] = @{PrimaryKeyColumn}";

    /// <summary>
    /// DELETE a row by primary key.
    /// </summary>
    public string DeleteCommand =>
        $"DELETE FROM [{TableName}] WHERE [{PrimaryKeyColumn}] = @{PrimaryKeyColumn}";

    /// <summary>
    /// Bulk INSERT — same as single insert; batching is handled at the adapter level.
    /// </summary>
    public string BulkInsertCommand => InsertCommand;

    /// <summary>
    /// Bulk UPDATE — same as single update; batching is handled at the adapter level.
    /// </summary>
    public string BulkUpdateCommand => UpdateCommand;

    /// <summary>
    /// Bulk DELETE — same as single delete; batching is handled at the adapter level.
    /// </summary>
    public string BulkDeleteCommand => DeleteCommand;

    /// <summary>
    /// RESET: Delete all data and tracking rows for the table.
    /// Used when a full re-sync is required.
    /// </summary>
    public string ResetCommand =>
        $"""
        DELETE FROM [{TrackingTableName}];
        DELETE FROM [{TableName}];
        """;

    /// <summary>
    /// SELECT metadata row from the tracking table for a given primary key.
    /// </summary>
    public string SelectMetadataCommand =>
        $"""
        SELECT [{PrimaryKeyColumn}], [update_scope_id], [timestamp], [sync_row_is_tombstone], [last_change_datetime]
        FROM [{TrackingTableName}]
        WHERE [{PrimaryKeyColumn}] = @{PrimaryKeyColumn}
        """;

    /// <summary>
    /// INSERT or UPDATE a metadata row in the tracking table.
    /// </summary>
    public string UpdateMetadataCommand =>
        $"""
        INSERT OR REPLACE INTO [{TrackingTableName}]
            ([{PrimaryKeyColumn}], [update_scope_id], [timestamp], [sync_row_is_tombstone], [last_change_datetime])
        VALUES
            (@{PrimaryKeyColumn}, @sync_scope_id, @sync_row_timestamp, @sync_row_is_tombstone, @sync_row_last_change)
        """;

    /// <summary>
    /// DELETE a metadata row from the tracking table.
    /// </summary>
    public string DeleteMetadataCommand =>
        $"DELETE FROM [{TrackingTableName}] WHERE [{PrimaryKeyColumn}] = @{PrimaryKeyColumn}";
}
