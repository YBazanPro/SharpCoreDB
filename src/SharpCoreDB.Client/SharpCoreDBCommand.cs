// <copyright file="SharpCoreDBCommand.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Google.Protobuf;
using SharpCoreDB.Server.Protocol;

namespace SharpCoreDB.Client;

/// <summary>
/// ADO.NET-like command for executing queries against SharpCoreDB server.
/// C# 14: Uses primary constructor for dependencies.
/// </summary>
public sealed class SharpCoreDBCommand(
    SharpCoreDBConnection connection,
    DatabaseService.DatabaseServiceClient client)
{
    private string? _commandText;
    private readonly Dictionary<string, object?> _parameters = [];
    private int _commandTimeout = 30000; // 30 seconds default

    /// <summary>
    /// Gets or sets the SQL command text.
    /// </summary>
    public string CommandText
    {
        get => _commandText ?? throw new InvalidOperationException("CommandText is not set");
        set => _commandText = value;
    }

    /// <summary>
    /// Gets or sets the command timeout (milliseconds).
    /// </summary>
    public int CommandTimeout
    {
        get => _commandTimeout;
        set => _commandTimeout = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>
    /// Gets the parameter collection.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    /// <summary>
    /// Adds a parameter to the command.
    /// </summary>
    public void AddParameter(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _parameters[name] = value;
    }

    /// <summary>
    /// Executes the query and returns a data reader.
    /// </summary>
    public async Task<SharpCoreDBDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
    {
        ValidateCommand();

        var request = CreateQueryRequest();
        var response = await client.ExecuteQueryAsync(request, cancellationToken: cancellationToken);

        return new SharpCoreDBDataReader(response);
    }

    /// <summary>
    /// Executes the query and returns rows affected.
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        ValidateCommand();

        var request = CreateQueryRequest();
        var response = await client.ExecuteQueryAsync(request, cancellationToken: cancellationToken);

        return response.RowsAffected;
    }

    /// <summary>
    /// Executes the query and returns the first column of the first row.
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        using var reader = await ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync())
        {
            return reader.GetValue(0);
        }

        return null;
    }

    private QueryRequest CreateQueryRequest()
    {
        var request = new QueryRequest
        {
            Sql = CommandText,
            TimeoutMs = _commandTimeout,
        };

        // Add parameters
        foreach (var (name, value) in _parameters)
        {
            request.Parameters[name] = SerializeParameter(value);
        }

        return request;
    }

    private static ByteString SerializeParameter(object? value)
    {
        // Placeholder: simple serialization
        // TODO: Implement proper type-aware serialization
        return value switch
        {
            null => ByteString.Empty,
            string s => ByteString.CopyFromUtf8(s),
            int i => ByteString.CopyFrom(BitConverter.GetBytes(i)),
            long l => ByteString.CopyFrom(BitConverter.GetBytes(l)),
            double d => ByteString.CopyFrom(BitConverter.GetBytes(d)),
            bool b => ByteString.CopyFrom([b ? (byte)1 : (byte)0]),
            _ => ByteString.CopyFromUtf8(value.ToString() ?? string.Empty),
        };
    }

    private void ValidateCommand()
    {
        if (string.IsNullOrWhiteSpace(_commandText))
        {
            throw new InvalidOperationException("CommandText is not set");
        }

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }
    }
}

/// <summary>
/// ADO.NET-like data reader for SharpCoreDB query results.
/// </summary>
public sealed class SharpCoreDBDataReader(QueryResponse response) : IAsyncDisposable
{
    private int _currentRowIndex = -1;

    /// <summary>Gets the number of columns.</summary>
    public int FieldCount => response.Columns.Count;

    /// <summary>Gets the number of rows affected.</summary>
    public int RowsAffected => response.RowsAffected;

    /// <summary>Gets the execution time.</summary>
    public double ExecutionTimeMs => response.ExecutionTimeMs;

    /// <summary>Advances to the next row.</summary>
    public Task<bool> ReadAsync()
    {
        _currentRowIndex++;
        return Task.FromResult(_currentRowIndex < response.Rows.Count);
    }

    /// <summary>Gets the value at the specified column index.</summary>
    public object? GetValue(int ordinal)
    {
        if (_currentRowIndex < 0 || _currentRowIndex >= response.Rows.Count)
        {
            throw new InvalidOperationException("No current row");
        }

        if (ordinal < 0 || ordinal >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        var row = response.Rows[_currentRowIndex];
        var column = response.Columns[ordinal];
        var bytes = row.Values[ordinal];

        // Placeholder: simple deserialization
        // TODO: Implement proper type-aware deserialization
        return column.Type switch
        {
            DataType.String => bytes.ToStringUtf8(),
            DataType.Integer => BitConverter.ToInt32(bytes.ToByteArray()),
            DataType.Long => BitConverter.ToInt64(bytes.ToByteArray()),
            DataType.Real => BitConverter.ToDouble(bytes.ToByteArray()),
            DataType.Boolean => bytes.ToByteArray()[0] != 0,
            _ => bytes.ToByteArray(),
        };
    }

    /// <summary>Gets the column name.</summary>
    public string GetName(int ordinal) => response.Columns[ordinal].Name;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Transaction for SharpCoreDB.
/// C# 14: Primary constructor for immutable transaction handle.
/// </summary>
public sealed class SharpCoreDBTransaction(
    SharpCoreDBConnection connection,
    DatabaseService.DatabaseServiceClient client,
    string transactionId) : IAsyncDisposable
{
    /// <summary>Gets the transaction ID.</summary>
    public string TransactionId { get; } = transactionId;

    /// <summary>Commits the transaction.</summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var handle = new TransactionHandle { TransactionId = TransactionId };
        await client.CommitTransactionAsync(handle, cancellationToken: cancellationToken);
    }

    /// <summary>Rolls back the transaction.</summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var handle = new TransactionHandle { TransactionId = TransactionId };
        await client.RollbackTransactionAsync(handle, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Auto-rollback if not committed
        try
        {
            await RollbackAsync();
        }
        catch
        {
            // Ignore errors on dispose
        }
    }
}
