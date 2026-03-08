// <copyright file="NonQueryAndTransactionTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using SharpCoreDB.Server.Protocol;
using CoreDatabaseService = SharpCoreDB.Server.Core.DatabaseService;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for non-query execution (INSERT/UPDATE/DELETE) and transaction lifecycle.
/// </summary>
public sealed class NonQueryAndTransactionTests : IAsyncLifetime
{
    private readonly TestServerFixture _fixture = new();
    private CoreDatabaseService _service = null!;
    private string _sessionId = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _service = _fixture.CreateDatabaseService();
        _sessionId = _fixture.TestSessionId!;

        await _fixture.ExecuteSetupSqlAsync("CREATE TABLE IF NOT EXISTS orders (id INTEGER, product TEXT, quantity INTEGER)");
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task ExecuteNonQuery_InsertRow_Succeeds()
    {
        // Arrange
        var request = new NonQueryRequest
        {
            SessionId = _sessionId,
            Sql = "INSERT INTO orders VALUES (1, 'Laptop', 2)",
        };

        // Act
        var response = await _service.ExecuteNonQuery(request, TestServerCallContext.Create());

        // Assert
        Assert.True(response.ExecutionTimeMs >= 0);
    }

    [Fact]
    public async Task ExecuteNonQuery_CreateTable_Succeeds()
    {
        // Arrange
        var request = new NonQueryRequest
        {
            SessionId = _sessionId,
            Sql = "CREATE TABLE IF NOT EXISTS products (id INTEGER, name TEXT, price REAL)",
        };

        // Act
        var response = await _service.ExecuteNonQuery(request, TestServerCallContext.Create());

        // Assert — should complete without error
        Assert.True(response.ExecutionTimeMs >= 0);
    }

    [Fact]
    public async Task ExecuteNonQuery_WithInvalidSession_ThrowsUnauthenticated()
    {
        // Arrange
        var request = new NonQueryRequest
        {
            SessionId = "bad-session",
            Sql = "INSERT INTO orders VALUES (99, 'Test', 1)",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _service.ExecuteNonQuery(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task BeginTransaction_ReturnsValidTransactionId()
    {
        // Arrange
        var request = new BeginTxRequest
        {
            SessionId = _sessionId,
            IsolationLevel = IsolationLevel.ReadCommitted,
        };

        // Act
        var response = await _service.BeginTransaction(request, TestServerCallContext.Create());

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.TransactionId));
        Assert.NotNull(response.StartTime);
    }

    [Fact]
    public async Task CommitTransaction_AfterBegin_Succeeds()
    {
        // Arrange — begin transaction
        var beginResponse = await _service.BeginTransaction(new BeginTxRequest
        {
            SessionId = _sessionId,
            IsolationLevel = IsolationLevel.ReadCommitted,
        }, TestServerCallContext.Create());

        // Act — commit
        var commitResponse = await _service.CommitTransaction(new CommitTxRequest
        {
            SessionId = _sessionId,
            TransactionId = beginResponse.TransactionId,
        }, TestServerCallContext.Create());

        // Assert
        Assert.True(commitResponse.Success);
    }

    [Fact]
    public async Task RollbackTransaction_AfterBegin_Succeeds()
    {
        // Arrange — begin transaction
        var beginResponse = await _service.BeginTransaction(new BeginTxRequest
        {
            SessionId = _sessionId,
            IsolationLevel = IsolationLevel.ReadCommitted,
        }, TestServerCallContext.Create());

        // Act — rollback
        var rollbackResponse = await _service.RollbackTransaction(new RollbackTxRequest
        {
            SessionId = _sessionId,
            TransactionId = beginResponse.TransactionId,
        }, TestServerCallContext.Create());

        // Assert
        Assert.True(rollbackResponse.Success);
    }

    [Fact]
    public async Task InsertThenSelect_DataPersists()
    {
        // Arrange — insert data
        await _service.ExecuteNonQuery(new NonQueryRequest
        {
            SessionId = _sessionId,
            Sql = "INSERT INTO orders VALUES (42, 'Keyboard', 5)",
        }, TestServerCallContext.Create());

        // Act — query it back
        var queryRequest = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELECT * FROM orders WHERE id = 42",
        };
        var responseWriter = new TestServerStreamWriter<QueryResponse>();
        await _service.ExecuteQuery(queryRequest, responseWriter, TestServerCallContext.Create());

        // Assert — should find the row
        var totalRows = responseWriter.Responses.Sum(r => r.Rows.Count);
        Assert.Equal(1, totalRows);
    }
}
