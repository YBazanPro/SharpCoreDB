// <copyright file="ErrorHandlingTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using SharpCoreDB.Server.Protocol;
using CoreDatabaseService = SharpCoreDB.Server.Core.DatabaseService;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for error handling, edge cases, and resilience.
/// </summary>
public sealed class ErrorHandlingTests : IAsyncLifetime
{
    private readonly TestServerFixture _fixture = new();
    private CoreDatabaseService _service = null!;
    private string _sessionId = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _service = _fixture.CreateDatabaseService();
        _sessionId = _fixture.TestSessionId!;
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task ExecuteQuery_EmptySql_ThrowsError()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act & Assert — empty SQL should throw (any exception is acceptable)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create()));
    }

    [Fact]
    public async Task ExecuteNonQuery_DropNonexistentTable_ThrowsInternalError()
    {
        // Arrange
        var request = new NonQueryRequest
        {
            SessionId = _sessionId,
            Sql = "DROP TABLE nonexistent_table_xyz",
        };

        // Act & Assert
        await Assert.ThrowsAsync<RpcException>(() =>
            _service.ExecuteNonQuery(request, TestServerCallContext.Create()));
    }

    [Fact]
    public async Task BeginTransaction_WithInvalidSession_ThrowsUnauthenticated()
    {
        // Arrange
        var request = new BeginTxRequest
        {
            SessionId = "invalid-session",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _service.BeginTransaction(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task CommitTransaction_WithInvalidSession_ThrowsUnauthenticated()
    {
        // Arrange
        var request = new CommitTxRequest
        {
            SessionId = "invalid-session",
            TransactionId = "tx-123",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _service.CommitTransaction(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task RollbackTransaction_WithInvalidSession_ThrowsUnauthenticated()
    {
        // Arrange
        var request = new RollbackTxRequest
        {
            SessionId = "invalid-session",
            TransactionId = "tx-123",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _service.RollbackTransaction(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task SessionManager_CreatesUniqueSessionIds()
    {
        // Arrange & Act
        var sessionIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var id = await _fixture.CreateSessionAsync("testdb");
            sessionIds.Add(id);
        }

        // Assert — all unique
        Assert.Equal(10, sessionIds.Distinct().Count());
    }

    [Fact]
    public async Task JwtTokenService_GenerateAndValidate_RoundTrips()
    {
        // Arrange
        var tokenService = _fixture.TokenService!;

        // Act
        var token = tokenService.GenerateToken("test-user", _sessionId, "admin,reader");
        var principal = tokenService.ValidateToken(token);

        // Assert
        var username = tokenService.GetUsernameFromToken(principal);
        var sessionId = tokenService.GetSessionIdFromToken(principal);

        Assert.Equal("test-user", username);
        Assert.Equal(_sessionId, sessionId);
    }
}
