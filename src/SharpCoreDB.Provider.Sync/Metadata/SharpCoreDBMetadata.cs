#nullable enable

using System.Data;
using Dotmim.Sync;
using Dotmim.Sync.Manager;

namespace SharpCoreDB.Provider.Sync.Metadata;

/// <summary>
/// Dotmim.Sync metadata provider for SharpCoreDB.
/// Maps SyncColumn definitions to SharpCoreDB-compatible types, precision, and validation.
/// Delegates type-mapping logic to <see cref="SharpCoreDBDbMetadata"/>.
/// </summary>
public sealed class SharpCoreDBMetadata : DbMetadata
{
    // SharpCoreDB type strings that correspond to DataType enum values.
    private static readonly HashSet<string> _numericTypes =
    [
        "INTEGER", "BIGINT", "REAL", "DECIMAL", "INT"
    ];

    private static readonly HashSet<string> _scalableTypes =
    [
        "REAL", "DECIMAL"
    ];

    /// <inheritdoc />
    public override bool IsValid(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);

        // A column is valid if it has a recognised SharpCoreDB / SQLite-style type string.
        var typeName = NormalizeTypeName(columnDefinition);
        return typeName switch
        {
            "INTEGER" or "INT" or "BIGINT" or "TEXT" or "REAL" or "BLOB"
                or "BOOLEAN" or "DECIMAL" or "GUID" or "DATETIME" => true,
            _ => false
        };
    }

    /// <inheritdoc />
    public override int GetMaxLength(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);

        if (columnDefinition.MaxLength > 0)
            return columnDefinition.MaxLength;

        var typeName = NormalizeTypeName(columnDefinition);
        return typeName switch
        {
            "TEXT" => 0,       // unlimited
            "BLOB" => 0,       // unlimited
            "GUID" => 36,      // standard GUID string length
            "DATETIME" => 33,  // ISO8601 with offset
            _ => 0
        };
    }

    /// <inheritdoc />
    public override object GetOwnerDbType(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);
        return NormalizeTypeName(columnDefinition);
    }

    /// <inheritdoc />
    public override DbType GetDbType(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);

        var typeName = NormalizeTypeName(columnDefinition);
        return typeName switch
        {
            "INTEGER" or "INT" => DbType.Int32,
            "BIGINT" => DbType.Int64,
            "TEXT" => DbType.String,
            "REAL" => DbType.Double,
            "BLOB" => DbType.Binary,
            "BOOLEAN" => DbType.Boolean,
            "DECIMAL" => DbType.Decimal,
            "GUID" => DbType.Guid,
            "DATETIME" => DbType.DateTime,
            _ => DbType.String
        };
    }

    /// <inheritdoc />
    public override bool IsReadonly(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);
        return columnDefinition.IsReadOnly || columnDefinition.IsCompute;
    }

    /// <inheritdoc />
    public override bool IsNumericType(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);
        return _numericTypes.Contains(NormalizeTypeName(columnDefinition));
    }

    /// <inheritdoc />
    public override bool IsSupportingScale(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);
        return _scalableTypes.Contains(NormalizeTypeName(columnDefinition));
    }

    /// <inheritdoc />
    public override (byte Precision, byte Scale) GetPrecisionAndScale(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);

        if (columnDefinition.PrecisionIsSpecified && columnDefinition.ScaleIsSpecified)
            return (columnDefinition.Precision, columnDefinition.Scale);

        var typeName = NormalizeTypeName(columnDefinition);
        return typeName switch
        {
            "DECIMAL" => (18, 6),
            "REAL" => (15, 0),
            _ => (0, 0)
        };
    }

    /// <inheritdoc />
    public override byte GetPrecision(SyncColumn columnDefinition)
    {
        return GetPrecisionAndScale(columnDefinition).Precision;
    }

    /// <inheritdoc />
    public override Type GetType(SyncColumn columnDefinition)
    {
        ArgumentNullException.ThrowIfNull(columnDefinition);

        var typeName = NormalizeTypeName(columnDefinition);
        return typeName switch
        {
            "INTEGER" or "INT" => typeof(int),
            "BIGINT" => typeof(long),
            "TEXT" => typeof(string),
            "REAL" => typeof(double),
            "BLOB" => typeof(byte[]),
            "BOOLEAN" => typeof(bool),
            "DECIMAL" => typeof(decimal),
            "GUID" => typeof(Guid),
            "DATETIME" => typeof(DateTime),
            _ => typeof(string)
        };
    }

    /// <summary>
    /// Normalises the column's type name to a canonical upper-case SharpCoreDB type string.
    /// Handles OriginalTypeName, OriginalDbType, and DataType fallbacks.
    /// </summary>
    private static string NormalizeTypeName(SyncColumn column)
    {
        // Prefer OriginalTypeName (set during schema discovery)
        var raw = column.OriginalTypeName;

        if (string.IsNullOrWhiteSpace(raw))
            raw = column.DataType;

        if (string.IsNullOrWhiteSpace(raw))
            return "TEXT";

        return raw.Trim().ToUpperInvariant() switch
        {
            "INT" or "INT32" or "INTEGER" or "SYSTEM.INT32" => "INTEGER",
            "LONG" or "INT64" or "BIGINT" or "SYSTEM.INT64" => "BIGINT",
            "STRING" or "TEXT" or "NVARCHAR" or "VARCHAR" or "SYSTEM.STRING" => "TEXT",
            "DOUBLE" or "FLOAT" or "REAL" or "SYSTEM.DOUBLE" => "REAL",
            "BYTE[]" or "BLOB" or "BINARY" or "VARBINARY" or "SYSTEM.BYTE[]" => "BLOB",
            "BOOL" or "BOOLEAN" or "SYSTEM.BOOLEAN" => "BOOLEAN",
            "DECIMAL" or "SYSTEM.DECIMAL" => "DECIMAL",
            "GUID" or "UNIQUEIDENTIFIER" or "SYSTEM.GUID" => "GUID",
            "DATETIME" or "DATETIME2" or "DATETIMEOFFSET" or "SYSTEM.DATETIME" => "DATETIME",
            var other => other
        };
    }
}
