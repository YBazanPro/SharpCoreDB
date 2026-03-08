// <copyright file="ConnectionLifecycleTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using SharpCoreDB.Server.Protocol;
using CoreDatabaseService = SharpCoreDB.Server.Core.DatabaseService;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for session/connection lifecycle: connect, ping, disconnect.
/// Validates the gRPC service layer handles sessions correctly.
/// </summary>
public sealed class ConnectionLifecycleTests : IAsyncLifetime
{
    private readonly TestServerFixture _fixture = new();
    private CoreDatabaseService _service = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _service = _fixture.CreateDatabaseService();
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task Connect_WithValidDatabase_ReturnsSuccess()
    {
        // Arrange
        var request = new ConnectRequest
        {
            DatabaseName = "testdb",
            UserName = "admin",
            Password = "admin123",
            ClientName = "IntegrationTest",
            ClientVersion = "1.0.0",
        };

        // Act
        var response = await _service.Connect(request, TestServerCallContext.Create());

        // Assert
        Assert.Equal(ConnectionStatus.Success, response.Status);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
        Assert.Equal("1.5.0", response.ServerVersion);
    }

    [Fact]
    public async Task Connect_WithEmptyDatabase_DefaultsToMaster()
    {
        // Arrange
        var request = new ConnectRequest
        {
            DatabaseName = "",
            UserName = "admin",
            Password = "admin123",
            ClientName = "IntegrationTest",
        };

        // Act
        var response = await _service.Connect(request, TestServerCallContext.Create());

        // Assert
        Assert.Equal(ConnectionStatus.Success, response.Status);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
    }

    [Fact]
    public async Task Connect_WithNonexistentDatabase_ReturnsDatabaseNotFound()
    {
        // Arrange
        var request = new ConnectRequest
        {
            DatabaseName = "nonexistent_db",
            UserName = "admin",
            Password = "admin123",
        };

        // Act
        var response = await _service.Connect(request, TestServerCallContext.Create());

        // Assert
        Assert.Equal(ConnectionStatus.DatabaseNotFound, response.Status);
    }

    [Fact]
    public async Task Disconnect_WithValidSession_Succeeds()
    {
        // Arrange — create session first
        var connectResponse = await _service.Connect(new ConnectRequest
        {
            DatabaseName = "testdb",
            UserName = "admin",
            Password = "admin123",
            ClientName = "IntegrationTest",
        }, TestServerCallContext.Create());

        // Act
        var disconnectResponse = await _service.Disconnect(new DisconnectRequest
        {
            SessionId = connectResponse.SessionId,
        }, TestServerCallContext.Create());

        // Assert
        Assert.True(disconnectResponse.Success);
    }

    [Fact]
    public async Task Ping_ReturnsServerTimeAndConnectionCount()
    {
        // Act
        var response = await _service.Ping(new PingRequest(), TestServerCallContext.Create());

        // Assert
        Assert.True(response.ServerTime > 0);
        Assert.True(response.ActiveConnections >= 0);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _service.HealthCheck(
            new HealthCheckRequest(), TestServerCallContext.Create());

        // Assert
        Assert.Equal(HealthStatus.Healthy, response.Status);
        Assert.Contains("healthy", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Connect_MultipleClients_EachGetsUniqueSession()
    {
        // Arrange & Act
        var sessions = new HashSet<string>();
        for (var i = 0; i < 5; i++)
        {
            var response = await _service.Connect(new ConnectRequest
            {
                DatabaseName = "testdb",
                UserName = "admin",
                Password = "admin123",
                ClientName = $"IntegrationTest-{i}",
            }, TestServerCallContext.Create());

            Assert.Equal(ConnectionStatus.Success, response.Status);
            sessions.Add(response.SessionId);
        }

        // Assert — all session IDs are unique
        Assert.Equal(5, sessions.Count);
    }
}
