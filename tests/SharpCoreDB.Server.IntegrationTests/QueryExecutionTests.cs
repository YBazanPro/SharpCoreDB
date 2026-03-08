// <copyright file="QueryExecutionTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using SharpCoreDB.Server.Protocol;
using CoreDatabaseService = SharpCoreDB.Server.Core.DatabaseService;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for SQL query execution through gRPC streaming.
/// Validates end-to-end: SQL → DatabaseService → result streaming.
/// </summary>
public sealed class QueryExecutionTests : IAsyncLifetime
{
    private readonly TestServerFixture _fixture = new();
    private CoreDatabaseService _service = null!;
    private string _sessionId = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _service = _fixture.CreateDatabaseService();
        _sessionId = _fixture.TestSessionId!;

        // Create test table with data
        await _fixture.ExecuteSetupSqlAsync("CREATE TABLE IF NOT EXISTS users (id INTEGER, name TEXT, age INTEGER)");
        await _fixture.ExecuteSetupSqlAsync("INSERT INTO users VALUES (1, 'Alice', 30)");
        await _fixture.ExecuteSetupSqlAsync("INSERT INTO users VALUES (2, 'Bob', 25)");
        await _fixture.ExecuteSetupSqlAsync("INSERT INTO users VALUES (3, 'Charlie', 35)");
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task ExecuteQuery_SelectAll_ReturnsAllRows()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELECT * FROM users",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act
        await _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create());

        // Assert
        var responses = responseWriter.Responses;
        Assert.NotEmpty(responses);

        // First frame is metadata
        var metadataFrame = responses[0];
        Assert.Equal(3, metadataFrame.RowsAffected);
        Assert.True(metadataFrame.Columns.Count >= 3);

        // Collect all rows across batches
        var totalRows = responses.Sum(r => r.Rows.Count);
        Assert.Equal(3, totalRows);
    }

    [Fact]
    public async Task ExecuteQuery_EmptyResult_ReturnsZeroRows()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELECT * FROM users WHERE age > 100",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act
        await _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create());

        // Assert
        var responses = responseWriter.Responses;
        Assert.NotEmpty(responses);

        var totalRows = responses.Sum(r => r.Rows.Count);
        Assert.Equal(0, totalRows);
    }

    [Fact]
    public async Task ExecuteQuery_WithInvalidSession_ThrowsUnauthenticated()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = "invalid-session-id",
            Sql = "SELECT 1",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task ExecuteQuery_WithInvalidSql_ThrowsOrReturnsEmpty()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELEC INVALID SYNTAX",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act — SharpCoreDB may throw or return empty depending on SQL
        try
        {
            await _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create());
            // If no exception, result should be empty
            var totalRows = responseWriter.Responses.Sum(r => r.Rows.Count);
            Assert.Equal(0, totalRows);
        }
        catch (RpcException ex)
        {
            Assert.Equal(StatusCode.Internal, ex.StatusCode);
        }
    }

    [Fact]
    public async Task ExecuteQuery_ColumnMetadata_IsCorrect()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELECT id, name FROM users",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act
        await _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create());

        // Assert — check column metadata in first frame
        var metadataFrame = responseWriter.Responses[0];
        Assert.True(metadataFrame.Columns.Count >= 2);

        var columnNames = metadataFrame.Columns.Select(c => c.Name).ToList();
        Assert.Contains("id", columnNames);
        Assert.Contains("name", columnNames);
    }

    [Fact]
    public async Task ExecuteQuery_HasMore_CorrectlySignalsBatching()
    {
        // Arrange
        var request = new QueryRequest
        {
            SessionId = _sessionId,
            Sql = "SELECT * FROM users",
        };

        var responseWriter = new TestServerStreamWriter<QueryResponse>();

        // Act
        await _service.ExecuteQuery(request, responseWriter, TestServerCallContext.Create());

        // Assert — last frame should have HasMore = false
        var lastFrame = responseWriter.Responses[^1];
        Assert.False(lastFrame.HasMore);
    }
}
