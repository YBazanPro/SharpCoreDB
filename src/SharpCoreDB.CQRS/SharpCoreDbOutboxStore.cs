// <copyright file="SharpCoreDbOutboxStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using System.Globalization;
using System.Text;
using SharpCoreDB.Interfaces;

/// <summary>
/// Persistent <see cref="IOutboxStore"/> backed by SharpCoreDB tables.
/// </summary>
/// <param name="database">SharpCoreDB database instance.</param>
/// <param name="tableName">Outbox table name.</param>
/// <param name="retryPolicy">Retry/dead-letter policy options.</param>
public sealed class SharpCoreDbOutboxStore(
    IDatabase database,
    string tableName = "scdb_outbox",
    OutboxRetryPolicyOptions? retryPolicy = null) : IOutboxStore
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly string _tableName = ValidateTableName(tableName);
    private readonly OutboxRetryPolicyOptions _retryPolicy = CreateRetryPolicy(retryPolicy);
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public Task<bool> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(message.MessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.MessageType);

        lock (_lock)
        {
            // Check for existing message in both outbox and dead-letter tables
            if (TryGetOutboxTable(out var outboxTable) && outboxTable.Select($"message_id = '{EscapeSqlLiteral(message.MessageId)}'").Count > 0)
            {
                return Task.FromResult(false);
            }

            if (_database.TryGetTable(_retryPolicy.DeadLetterTableName, out var dlTable) && dlTable.Select($"message_id = '{EscapeSqlLiteral(message.MessageId)}'").Count > 0)
            {
                return Task.FromResult(false);
            }

            EnsureSchema();

            var payloadHex = Convert.ToHexString(message.Payload.Span);
            var createdAt = message.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var hasRetryMetadata = TryGetOutboxTable(out var table) && HasRetryMetadataColumns(table);

            var insertSql = hasRetryMetadata
                ? $"INSERT INTO {_tableName} VALUES ('{EscapeSqlLiteral(message.MessageId)}', '{EscapeSqlLiteral(message.AggregateId)}', '{EscapeSqlLiteral(message.MessageType)}', '{payloadHex}', '{createdAt}', 0, '', '{createdAt}')"
                : $"INSERT INTO {_tableName} VALUES ('{EscapeSqlLiteral(message.MessageId)}', '{EscapeSqlLiteral(message.AggregateId)}', '{EscapeSqlLiteral(message.MessageType)}', '{payloadHex}', '{createdAt}')";

            _database.ExecuteBatchSQL([insertSql]);
            _database.Flush();
            _database.ForceSave();
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        }

        lock (_lock)
        {
            EnsureSchema();

            var hasSchedulingMetadata = TryGetOutboxTable(out var table)
                && HasColumn(table, "next_attempt_utc");

            var query = hasSchedulingMetadata
                ? $"SELECT message_id, aggregate_id, message_type, payload_hex, created_at_utc, attempt_count, last_error, next_attempt_utc FROM {_tableName} ORDER BY created_at_utc ASC"
                : $"SELECT message_id, aggregate_id, message_type, payload_hex, created_at_utc FROM {_tableName} ORDER BY created_at_utc ASC";

            var rows = _database.ExecuteQuery(query, new Dictionary<string, object?>());

            var nowUtc = DateTimeOffset.UtcNow;
            List<OutboxMessage> messages = [];
            foreach (var row in rows)
            {
                if (hasSchedulingMetadata && GetNextAttemptUtc(row) > nowUtc)
                {
                    continue;
                }

                messages.Add(MapMessage(row));
                if (messages.Count >= limit)
                {
                    break;
                }
            }

            return Task.FromResult<IReadOnlyList<OutboxMessage>>(messages);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        }

        lock (_lock)
        {
            if (!_database.TryGetTable(_retryPolicy.DeadLetterTableName, out _))
            {
                return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
            }

            var rows = _database.ExecuteQuery(
                $"SELECT message_id, aggregate_id, message_type, payload_hex, created_at_utc FROM {_retryPolicy.DeadLetterTableName} ORDER BY failed_at_utc ASC LIMIT {limit}",
                new Dictionary<string, object?>());

            List<OutboxMessage> messages = [];
            foreach (var row in rows)
            {
                messages.Add(MapMessage(row));
            }

            return Task.FromResult<IReadOnlyList<OutboxMessage>>(messages);
        }
    }

    /// <inheritdoc />
    public Task RequeueDeadLetterAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        lock (_lock)
        {
            if (!_database.TryGetTable(_retryPolicy.DeadLetterTableName, out _))
            {
                return Task.CompletedTask;
            }

            var rows = _database.ExecuteQuery(
                $"SELECT message_id, aggregate_id, message_type, payload_hex, created_at_utc FROM {_retryPolicy.DeadLetterTableName} WHERE message_id = '{EscapeSqlLiteral(messageId)}'",
                new Dictionary<string, object?>());

            if (rows.Count == 0)
            {
                return Task.CompletedTask;
            }

            var row = rows[0];
            EnsureSchema();

            var nowUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var escapedMessageId = EscapeSqlLiteral(messageId);
            var aggregateId = EscapeSqlLiteral(GetStringValue(row, "aggregate_id"));
            var messageType = EscapeSqlLiteral(GetStringValue(row, "message_type"));
            var payloadHex = EscapeSqlLiteral(GetStringValue(row, "payload_hex"));
            var createdAtUtc = EscapeSqlLiteral(GetStringValue(row, "created_at_utc"));

            _database.ExecuteBatchSQL([
                $"INSERT INTO {_tableName} VALUES ('{escapedMessageId}', '{aggregateId}', '{messageType}', '{payloadHex}', '{createdAtUtc}', 0, '', '{nowUtc}')"
            ]);

            if (_database.TryGetTable(_retryPolicy.DeadLetterTableName, out var dlTable))
            {
                dlTable.Delete($"message_id = '{escapedMessageId}'");
            }

            _database.Flush();
            _database.ForceSave();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkPublishedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        lock (_lock)
        {
            EnsureSchema();

            if (_database.TryGetTable(_tableName, out var table))
            {
                table.Delete($"message_id = '{EscapeSqlLiteral(messageId)}'");
            }

            _database.Flush();
            _database.ForceSave();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordFailureAsync(string messageId, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        lock (_lock)
        {
            EnsureSchema();

            if (!TryGetOutboxTable(out var table) || !HasRetryMetadataColumns(table))
            {
                return Task.CompletedTask;
            }

            var rows = _database.ExecuteQuery(
                $"SELECT message_id, aggregate_id, message_type, payload_hex, created_at_utc, attempt_count FROM {_tableName} WHERE message_id = '{EscapeSqlLiteral(messageId)}' LIMIT 1",
                new Dictionary<string, object?>());

            if (rows.Count == 0)
            {
                return Task.CompletedTask;
            }

            var row = rows[0];
            var attemptCount = checked((int)Math.Clamp(GetLongValue(row, "attempt_count") + 1, 1, int.MaxValue));

            if (attemptCount >= _retryPolicy.MaxAttempts)
            {
                EnsureDeadLetterSchema();
                var aggregateId = EscapeSqlLiteral(GetStringValue(row, "aggregate_id"));
                var messageType = EscapeSqlLiteral(GetStringValue(row, "message_type"));
                var payloadHex = EscapeSqlLiteral(GetStringValue(row, "payload_hex"));
                var createdAtUtc = EscapeSqlLiteral(GetStringValue(row, "created_at_utc"));
                var failedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

                _database.ExecuteBatchSQL([
                    $"INSERT INTO {_retryPolicy.DeadLetterTableName} VALUES ('{EscapeSqlLiteral(messageId)}', '{aggregateId}', '{messageType}', '{payloadHex}', '{createdAtUtc}', {attemptCount}, '{EscapeSqlLiteral(error)}', '{failedAtUtc}')"
                ]);

                table.Delete($"message_id = '{EscapeSqlLiteral(messageId)}'");
            }
            else
            {
                var nextAttemptUtc = DateTimeOffset.UtcNow.Add(GetBackoff(attemptCount));
                table.Update(
                    $"message_id = '{EscapeSqlLiteral(messageId)}'",
                    new Dictionary<string, object>
                    {
                        ["attempt_count"] = attemptCount,
                        ["last_error"] = error,
                        ["next_attempt_utc"] = nextAttemptUtc.ToString("O", CultureInfo.InvariantCulture),
                    });
            }

            _database.Flush();
            _database.ForceSave();
        }

        return Task.CompletedTask;
    }

    private void EnsureSchema()
    {
        if (!_database.TryGetTable(_tableName, out _))
        {
            _database.ExecuteSQL($"CREATE TABLE {_tableName} (message_id TEXT PRIMARY KEY, aggregate_id TEXT, message_type TEXT, payload_hex TEXT, created_at_utc TEXT, attempt_count INTEGER, last_error TEXT, next_attempt_utc TEXT)");
            _database.Flush();
            _database.ForceSave();
        }
    }

    private void EnsureDeadLetterSchema()
    {
        if (_database.TryGetTable(_retryPolicy.DeadLetterTableName, out _))
        {
            return;
        }

        _database.ExecuteSQL($"CREATE TABLE {_retryPolicy.DeadLetterTableName} (message_id TEXT PRIMARY KEY, aggregate_id TEXT, message_type TEXT, payload_hex TEXT, created_at_utc TEXT, attempt_count INTEGER, last_error TEXT, failed_at_utc TEXT)");
        _database.Flush();
        _database.ForceSave();
    }

    private bool TryGetOutboxTable(out ITable table) => _database.TryGetTable(_tableName, out table);

    private static bool HasRetryMetadataColumns(ITable table) =>
        HasColumn(table, "attempt_count")
        && HasColumn(table, "last_error")
        && HasColumn(table, "next_attempt_utc");

    private static bool HasColumn(ITable table, string columnName) =>
        table.Columns.Any(column => string.Equals(column, columnName, StringComparison.OrdinalIgnoreCase));

    private static OutboxMessage MapMessage(Dictionary<string, object> row)
    {
        var messageId = GetStringValue(row, "message_id");
        var aggregateId = GetStringValue(row, "aggregate_id");
        var messageType = GetStringValue(row, "message_type");
        var payloadHex = GetStringValue(row, "payload_hex");
        var createdAtRaw = GetStringValue(row, "created_at_utc");

        var payload = payloadHex.Length > 0
            ? Convert.FromHexString(payloadHex)
            : [];

        var createdAtUtc = DateTimeOffset.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        // Rows in the table are always unpublished; IsPublished is false by definition.
        return new OutboxMessage(messageId, aggregateId, messageType, payload, createdAtUtc, false);
    }

    private static OutboxRetryPolicyOptions CreateRetryPolicy(OutboxRetryPolicyOptions? retryPolicy)
    {
        var options = retryPolicy ?? new OutboxRetryPolicyOptions();

        if (options.BaseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException("OutboxRetryPolicyOptions.BaseDelay must be greater than zero.", nameof(retryPolicy));
        }

        if (options.MaxDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException("OutboxRetryPolicyOptions.MaxDelay must be greater than zero.", nameof(retryPolicy));
        }

        if (options.MaxAttempts <= 0)
        {
            throw new ArgumentException("OutboxRetryPolicyOptions.MaxAttempts must be greater than zero.", nameof(retryPolicy));
        }

        options.DeadLetterTableName = ValidateTableName(options.DeadLetterTableName);
        return options;
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

        // Case-insensitive fallback for column name variations
        foreach (var kvp in row)
        {
            if (string.Equals(kvp.Key, columnName, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static DateTimeOffset GetNextAttemptUtc(Dictionary<string, object> row)
    {
        var nextAttemptRaw = GetStringValue(row, "next_attempt_utc");
        if (DateTimeOffset.TryParse(nextAttemptRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedNextAttempt))
        {
            return parsedNextAttempt;
        }

        var createdAtRaw = GetStringValue(row, "created_at_utc");
        if (DateTimeOffset.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreatedAt))
        {
            return parsedCreatedAt;
        }

        return DateTimeOffset.MinValue;
    }

    private TimeSpan GetBackoff(int attemptCount)
    {
        var boundedAttempt = Math.Clamp(attemptCount, 1, 30);
        var seconds = _retryPolicy.BaseDelay.TotalSeconds * Math.Pow(2, boundedAttempt - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, _retryPolicy.MaxDelay.TotalSeconds));
    }
}
