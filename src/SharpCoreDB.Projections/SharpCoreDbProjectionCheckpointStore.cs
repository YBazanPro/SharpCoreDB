// <copyright file="SharpCoreDbProjectionCheckpointStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using System.Globalization;
using SharpCoreDB.Interfaces;

/// <summary>
/// Persistent <see cref="IProjectionCheckpointStore"/> backed by SharpCoreDB tables.
/// </summary>
/// <param name="database">SharpCoreDB database instance.</param>
/// <param name="tableName">Checkpoint table name.</param>
public sealed class SharpCoreDbProjectionCheckpointStore(
    IDatabase database,
    string tableName = "scdb_projection_checkpoints") : IProjectionCheckpointStore
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly string _tableName = ValidateTableName(tableName);
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionName,
        string databaseId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        lock (_lock)
        {
            EnsureSchema();

            var rows = _database.ExecuteQuery(
                $"SELECT projection_name, database_id, tenant_id, global_sequence, updated_at_utc FROM {_tableName} WHERE projection_name = ? AND database_id = ? AND tenant_id = ? LIMIT 1",
                new Dictionary<string, object?>
                {
                    ["0"] = projectionName,
                    ["1"] = databaseId,
                    ["2"] = tenantId,
                });

            if (rows.Count == 0)
            {
                return Task.FromResult<ProjectionCheckpoint?>(null);
            }

            return Task.FromResult<ProjectionCheckpoint?>(MapCheckpoint(rows[0]));
        }
    }

    /// <inheritdoc />
    public Task SaveCheckpointAsync(
        ProjectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.ProjectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.DatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.TenantId);

        lock (_lock)
        {
            EnsureSchema();

            var updatedAt = checkpoint.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            _database.ExecuteBatchSQL(
            [
                $"DELETE FROM {_tableName} WHERE projection_name = '{EscapeSqlLiteral(checkpoint.ProjectionName)}' AND database_id = '{EscapeSqlLiteral(checkpoint.DatabaseId)}' AND tenant_id = '{EscapeSqlLiteral(checkpoint.TenantId)}'",
                $"INSERT INTO {_tableName} VALUES ('{EscapeSqlLiteral(checkpoint.ProjectionName)}', '{EscapeSqlLiteral(checkpoint.DatabaseId)}', '{EscapeSqlLiteral(checkpoint.TenantId)}', {checkpoint.GlobalSequence}, '{updatedAt}')"
            ]);
            _database.Flush();
            _database.ForceSave();

            return Task.CompletedTask;
        }
    }

    private void EnsureSchema()
    {
        if (!_database.TryGetTable(_tableName, out _))
        {
            _database.ExecuteSQL($"CREATE TABLE {_tableName} (projection_name TEXT, database_id TEXT, tenant_id TEXT, global_sequence LONG, updated_at_utc TEXT)");
            _database.Flush();
            _database.ForceSave();
        }
    }

    private static ProjectionCheckpoint MapCheckpoint(Dictionary<string, object> row)
    {
        var projectionName = GetStringValue(row, "projection_name");
        var databaseId = GetStringValue(row, "database_id");
        var tenantId = GetStringValue(row, "tenant_id");
        var globalSequence = GetLongValue(row, "global_sequence");
        var updatedAtRaw = GetStringValue(row, "updated_at_utc");

        var updatedAtUtc = DateTimeOffset.TryParse(updatedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        return new ProjectionCheckpoint(
            projectionName,
            databaseId,
            tenantId,
            globalSequence,
            updatedAtUtc);
    }

    private static string ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        foreach (var character in tableName)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                throw new ArgumentException("Table name can only contain letters, digits, and underscore.", nameof(tableName));
            }
        }

        return tableName;
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static long GetLongValue(Dictionary<string, object> row, string columnName)
    {
        if (!TryGetColumnValue(row, columnName, out var value) || value is null)
        {
            return 0;
        }

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static string GetStringValue(Dictionary<string, object> row, string columnName)
    {
        if (!TryGetColumnValue(row, columnName, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool TryGetColumnValue(Dictionary<string, object> row, string columnName, out object? value)
    {
        if (row.TryGetValue(columnName, out var exactValue))
        {
            value = exactValue;
            return true;
        }

        foreach (var pair in row)
        {
            if (string.Equals(pair.Key, columnName, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
