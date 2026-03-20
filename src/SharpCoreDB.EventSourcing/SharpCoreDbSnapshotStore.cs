// <copyright file="SharpCoreDbSnapshotStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

using System.Globalization;
using SharpCoreDB.Interfaces;

/// <summary>
/// Persistent <see cref="ISnapshotStore"/> backed by a SharpCoreDB table.
/// Snapshots are stored per-stream sorted by version with Base64-encoded payloads.
/// </summary>
/// <param name="database">The SharpCoreDB database instance.</param>
/// <param name="tableName">Table name for snapshot storage.</param>
public sealed class SharpCoreDbSnapshotStore(IDatabase database, string tableName = "scdb_snapshots") : ISnapshotStore
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly string _tableName = ValidateTableName(tableName);
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public Task SaveAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            EnsureSchema();

            var escapedStreamId = EscapeSqlLiteral(snapshot.StreamId.Value);
            var snapshotDataBase64 = Convert.ToBase64String(snapshot.SnapshotData.ToArray());
            var createdAtText = snapshot.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

            // DELETE existing snapshot for this stream+version (upsert), then INSERT new one
            // Use ExecuteSQL for DELETE to ensure PageBased storage engine properly removes rows
            _database.ExecuteSQL(
                $"DELETE FROM {_tableName} WHERE stream_id = '{escapedStreamId}' AND version = {snapshot.Version}");
            _database.Flush();
            _database.ForceSave();

            _database.ExecuteBatchSQL(
                [$"INSERT INTO {_tableName} VALUES ('{escapedStreamId}', {snapshot.Version}, '{snapshotDataBase64}', '{createdAtText}')"]);
            _database.Flush();
            _database.ForceSave();
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<EventSnapshot?> LoadLatestAsync(
        EventStreamId streamId,
        long maxVersion = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            EnsureSchema();

            if (!_database.TryGetTable(_tableName, out var table))
            {
                return Task.FromResult<EventSnapshot?>(null);
            }

            var rows = table.Select();
            if (rows.Count == 0)
            {
                return Task.FromResult<EventSnapshot?>(null);
            }

            Dictionary<string, object>? bestRow = null;
            long bestVersion = long.MinValue;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowStreamId = GetStringValue(row, "stream_id");
                var version = GetLongValue(row, "version");
                if (!string.Equals(rowStreamId, streamId.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (version <= maxVersion && version > bestVersion)
                {
                    bestVersion = version;
                    bestRow = row;
                }
            }

            if (bestRow is null)
            {
                return Task.FromResult<EventSnapshot?>(null);
            }

            return Task.FromResult<EventSnapshot?>(MapSnapshot(bestRow));
        }
    }

    /// <inheritdoc />
    public Task<int> DeleteAllAsync(EventStreamId streamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            EnsureSchema();

            // Count existing snapshots before deletion
            var countRows = _database.ExecuteQuery(
                $"SELECT version FROM {_tableName} WHERE stream_id = ?",
                new Dictionary<string, object?> { ["0"] = streamId.Value });

            var count = countRows.Count;
            if (count > 0)
            {
                // Use ExecuteSQL for DELETE to ensure PageBased storage engine properly removes rows
                _database.ExecuteSQL(
                    $"DELETE FROM {_tableName} WHERE stream_id = '{EscapeSqlLiteral(streamId.Value)}'");
                _database.Flush();
                _database.ForceSave();
            }

            return Task.FromResult(count);
        }
    }

    private void EnsureSchema()
    {
        if (!_database.TryGetTable(_tableName, out _))
        {
            _database.ExecuteSQL(
                $"CREATE TABLE {_tableName} (stream_id TEXT, version LONG, snapshot_data_base64 TEXT, created_at_utc TEXT) STORAGE = PAGE_BASED");
            _database.Flush();
            _database.ForceSave();
        }
    }

    private static EventSnapshot MapSnapshot(Dictionary<string, object> row)
    {
        var streamId = GetStringValue(row, "stream_id");
        var version = GetLongValue(row, "version");
        var dataBase64 = GetStringValue(row, "snapshot_data_base64");
        var createdAtRaw = GetStringValue(row, "created_at_utc");

        var createdAt = DateTimeOffset.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        return new EventSnapshot(
            new EventStreamId(streamId),
            version,
            DecodeBase64(dataBase64),
            createdAt);
    }

    private static byte[] DecodeBase64(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return Convert.FromBase64String(value);
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

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

    private static string GetStringValue(Dictionary<string, object> row, string columnName)
    {
        if (!TryGetColumnValue(row, columnName, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static long GetLongValue(Dictionary<string, object> row, string columnName)
    {
        if (!TryGetColumnValue(row, columnName, out var value) || value is null)
        {
            return 0;
        }

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
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

            var dotIndex = pair.Key.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex + 1 < pair.Key.Length)
            {
                var suffix = pair.Key[(dotIndex + 1)..];
                if (string.Equals(suffix, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }
}
