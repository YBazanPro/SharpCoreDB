// <copyright file="SharpCoreDBConnection.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Net.Client;
using SharpCoreDB.Server.Protocol;

namespace SharpCoreDB.Client;

/// <summary>
/// ADO.NET-like connection to SharpCoreDB network server.
/// C# 14: Uses primary constructor for immutability.
/// </summary>
public sealed class SharpCoreDBConnection : IAsyncDisposable
{
    private readonly string _connectionString;
    private GrpcChannel? _channel;
    private DatabaseService.DatabaseServiceClient? _client;
    private ConnectionState _state = ConnectionState.Closed;
    private readonly Lock _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SharpCoreDBConnection"/> class.
    /// </summary>
    /// <param name="connectionString">
    /// Connection string format: "Server=localhost;Port=5001;Database=mydb;SSL=true;Username=admin;Password=***"
    /// </param>
    public SharpCoreDBConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        ConnectionStringParser = new ConnectionStringParser(connectionString);
    }

    /// <summary>
    /// Gets the connection string parser.
    /// </summary>
    public ConnectionStringParser ConnectionStringParser { get; }

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState State => _state;

    /// <summary>
    /// Gets the server version (available after connection).
    /// </summary>
    public string? ServerVersion { get; private set; }

    /// <summary>
    /// Opens the connection to the SharpCoreDB server.
    /// </summary>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (_state != ConnectionState.Closed)
            {
                throw new InvalidOperationException($"Connection is already {_state}");
            }

            _state = ConnectionState.Connecting;
        }

        try
        {
            // Create gRPC channel
            var address = ConnectionStringParser.ServerAddress;
            var channelOptions = new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 100 * 1024 * 1024, // 100MB
                MaxSendMessageSize = 100 * 1024 * 1024,
            };

            _channel = GrpcChannel.ForAddress(address, channelOptions);
            _client = new DatabaseService.DatabaseServiceClient(_channel);

            // Health check to verify connection
            var healthRequest = new HealthCheckRequest { Detailed = false };
            var healthResponse = await _client.HealthCheckAsync(healthRequest, cancellationToken: cancellationToken);

            ServerVersion = healthResponse.Version;

            lock (_stateLock)
            {
                _state = ConnectionState.Open;
            }
        }
        catch
        {
            lock (_stateLock)
            {
                _state = ConnectionState.Closed;
            }

            await CleanupResourcesAsync();
            throw;
        }
    }

    /// <summary>
    /// Closes the connection.
    /// </summary>
    public async Task CloseAsync()
    {
        lock (_stateLock)
        {
            if (_state == ConnectionState.Closed)
            {
                return;
            }

            _state = ConnectionState.Closed;
        }

        await CleanupResourcesAsync();
    }

    /// <summary>
    /// Creates a new command for this connection.
    /// </summary>
    public SharpCoreDBCommand CreateCommand()
    {
        if (_state != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }

        if (_client is null)
        {
            throw new InvalidOperationException("Client not initialized");
        }

        return new SharpCoreDBCommand(this, _client);
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    public async Task<SharpCoreDBTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (_state != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection is not open");
        }

        if (_client is null)
        {
            throw new InvalidOperationException("Client not initialized");
        }

        var txOptions = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            TimeoutMs = 300000, // 5 minutes default
        };

        var handle = await _client.BeginTransactionAsync(txOptions, cancellationToken: cancellationToken);
        return new SharpCoreDBTransaction(this, _client, handle.TransactionId);
    }

    private async Task CleanupResourcesAsync()
    {
        if (_channel is not null)
        {
            await _channel.ShutdownAsync();
            _channel.Dispose();
            _channel = null;
        }

        _client = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}

/// <summary>
/// Connection state enumeration.
/// </summary>
public enum ConnectionState
{
    Closed,
    Connecting,
    Open,
    Executing,
    Fetching,
}

/// <summary>
/// Parses SharpCoreDB connection strings.
/// C# 14: Primary constructor for immutable parser.
/// </summary>
public sealed class ConnectionStringParser(string connectionString)
{
    private readonly Dictionary<string, string> _parameters = ParseConnectionString(connectionString);

    /// <summary>Gets the server address (e.g., https://localhost:5001).</summary>
    public string ServerAddress
    {
        get
        {
            var server = GetParameter("Server", "localhost");
            var port = GetParameter("Port", "5001");
            var useSsl = GetParameter("SSL", "true").Equals("true", StringComparison.OrdinalIgnoreCase);
            var protocol = useSsl ? "https" : "http";
            return $"{protocol}://{server}:{port}";
        }
    }

    /// <summary>Gets the database name.</summary>
    public string? Database => GetParameter("Database");

    /// <summary>Gets the username.</summary>
    public string? Username => GetParameter("Username");

    /// <summary>Gets the password.</summary>
    public string? Password => GetParameter("Password");

    private string? GetParameter(string key, string? defaultValue = null)
    {
        return _parameters.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        return result;
    }
}
