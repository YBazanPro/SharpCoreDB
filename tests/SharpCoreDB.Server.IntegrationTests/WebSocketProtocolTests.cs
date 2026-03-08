// <copyright file="WebSocketProtocolTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharpCoreDB.Server.Core;
using SharpCoreDB.Server.Core.Security;
using SharpCoreDB.Server.Core.WebSockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WsMsgType = SharpCoreDB.Server.Core.WebSockets.WebSocketMessageType;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for WebSocket protocol types, serialization, and handler logic.
/// </summary>
public sealed class WebSocketProtocolTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;

    public WebSocketProtocolTests(TestServerFixture fixture) => _fixture = fixture;

    // ── Protocol Serialization ──

    [Fact]
    public void WebSocketRequest_AuthMessage_SerializesCorrectly()
    {
        var request = new WebSocketRequest
        {
            Type = WsMsgType.Auth,
            Id = "req-1",
            Token = "jwt-token-here",
        };

        var json = JsonSerializer.Serialize(request, WebSocketJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<WebSocketRequest>(json, WebSocketJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal(WsMsgType.Auth, deserialized.Type);
        Assert.Equal("req-1", deserialized.Id);
        Assert.Equal("jwt-token-here", deserialized.Token);
    }

    [Fact]
    public void WebSocketRequest_QueryMessage_SerializesCorrectly()
    {
        var request = new WebSocketRequest
        {
            Type = WsMsgType.Query,
            Id = "req-2",
            Sql = "SELECT * FROM users",
            Database = "testdb",
        };

        var json = JsonSerializer.Serialize(request, WebSocketJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<WebSocketRequest>(json, WebSocketJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal(WsMsgType.Query, deserialized.Type);
        Assert.Equal("SELECT * FROM users", deserialized.Sql);
        Assert.Equal("testdb", deserialized.Database);
    }

    [Fact]
    public void WebSocketRequest_BatchMessage_SerializesCorrectly()
    {
        var request = new WebSocketRequest
        {
            Type = WsMsgType.Batch,
            Id = "req-3",
            Statements = ["INSERT INTO t1 VALUES (1)", "INSERT INTO t1 VALUES (2)"],
        };

        var json = JsonSerializer.Serialize(request, WebSocketJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<WebSocketRequest>(json, WebSocketJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal(WsMsgType.Batch, deserialized.Type);
        Assert.Equal(2, deserialized.Statements!.Count);
    }

    [Fact]
    public void WebSocketResponse_ResultMessage_SerializesCorrectly()
    {
        var response = new WebSocketResponse
        {
            Type = WsMsgType.Result,
            Id = "req-4",
            Success = true,
            Columns = ["id", "name"],
            Rows =
            [
                new Dictionary<string, object?> { ["id"] = 1, ["name"] = "Alice" },
                new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Bob" },
            ],
            HasMore = false,
        };

        var json = JsonSerializer.Serialize(response, WebSocketJsonOptions.Default);
        Assert.Contains("\"columns\"", json);
        Assert.Contains("\"rows\"", json);
        Assert.Contains("Alice", json);

        var deserialized = JsonSerializer.Deserialize<WebSocketResponse>(json, WebSocketJsonOptions.Default);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Rows!.Count);
        Assert.False(deserialized.HasMore);
    }

    [Fact]
    public void WebSocketResponse_ErrorMessage_SerializesCorrectly()
    {
        var response = new WebSocketResponse
        {
            Type = WsMsgType.Error,
            Id = "req-5",
            Success = false,
            Error = "Something went wrong",
            Code = "QUERY_ERROR",
        };

        var json = JsonSerializer.Serialize(response, WebSocketJsonOptions.Default);
        Assert.Contains("QUERY_ERROR", json);

        var deserialized = JsonSerializer.Deserialize<WebSocketResponse>(json, WebSocketJsonOptions.Default);
        Assert.NotNull(deserialized);
        Assert.False(deserialized.Success);
        Assert.Equal("QUERY_ERROR", deserialized.Code);
    }

    [Fact]
    public void WebSocketResponse_NullFieldsOmitted()
    {
        var response = new WebSocketResponse
        {
            Type = WsMsgType.Pong,
            Id = "req-6",
            Success = true,
        };

        var json = JsonSerializer.Serialize(response, WebSocketJsonOptions.Default);

        // Null fields should be omitted (WhenWritingNull)
        Assert.DoesNotContain("\"rows\"", json);
        Assert.DoesNotContain("\"error\"", json);
        Assert.DoesNotContain("\"columns\"", json);
    }

    // ── WsMsgType Enum ──

    [Theory]
    [InlineData(WsMsgType.Auth, "\"Auth\"")]
    [InlineData(WsMsgType.Query, "\"Query\"")]
    [InlineData(WsMsgType.Execute, "\"Execute\"")]
    [InlineData(WsMsgType.Batch, "\"Batch\"")]
    [InlineData(WsMsgType.Result, "\"Result\"")]
    [InlineData(WsMsgType.Error, "\"Error\"")]
    [InlineData(WsMsgType.Ping, "\"Ping\"")]
    [InlineData(WsMsgType.Pong, "\"Pong\"")]
    public void WsMsgType_SerializesAsString(WsMsgType type, string expected)
    {
        var json = JsonSerializer.Serialize(type, WebSocketJsonOptions.Default);
        Assert.Equal(expected, json);
    }

    [Theory]
    [InlineData("\"Auth\"", WsMsgType.Auth)]
    [InlineData("\"Query\"", WsMsgType.Query)]
    [InlineData("\"Result\"", WsMsgType.Result)]
    public void WsMsgType_DeserializesFromString(string json, WsMsgType expected)
    {
        var result = JsonSerializer.Deserialize<WsMsgType>(json, WebSocketJsonOptions.Default);
        Assert.Equal(expected, result);
    }

    // ── Handler (via InProcessWebSocket pair) ──

    [Fact]
    public async Task Handler_PingWithoutAuth_ReturnsPong()
    {
        // Arrange
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        // Act — send Ping
        var ping = new WebSocketRequest
        {
            Type = WsMsgType.Ping,
            Id = "ping-1",
        };

        await SendJsonAsync(client, ping);
        var response = await ReceiveJsonAsync<WebSocketResponse>(client);

        // Assert
        Assert.Equal(WsMsgType.Pong, response.Type);
        Assert.Equal("ping-1", response.Id);
        Assert.True(response.Success);

        // Close
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    [Fact]
    public async Task Handler_QueryWithoutAuth_ReturnsUnauthenticated()
    {
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        var query = new WebSocketRequest
        {
            Type = WsMsgType.Query,
            Id = "q-1",
            Sql = "SELECT 1",
        };

        await SendJsonAsync(client, query);
        var response = await ReceiveJsonAsync<WebSocketResponse>(client);

        Assert.Equal(WsMsgType.Error, response.Type);
        Assert.Equal("UNAUTHENTICATED", response.Code);
        Assert.False(response.Success);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    [Fact]
    public async Task Handler_AuthWithValidToken_ReturnsAck()
    {
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        // Generate a valid JWT token
        var token = _fixture.TokenService!.GenerateToken("test-user", "ws-session-1", "admin");

        var auth = new WebSocketRequest
        {
            Type = WsMsgType.Auth,
            Id = "auth-1",
            Token = token,
            Database = "testdb",
        };

        await SendJsonAsync(client, auth);
        var response = await ReceiveJsonAsync<WebSocketResponse>(client);

        Assert.Equal(WsMsgType.Ack, response.Type);
        Assert.True(response.Success);
        Assert.Contains("test-user", response.Message);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    [Fact]
    public async Task Handler_AuthWithInvalidToken_ReturnsError()
    {
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        var auth = new WebSocketRequest
        {
            Type = WsMsgType.Auth,
            Id = "auth-2",
            Token = "invalid-jwt-token",
        };

        await SendJsonAsync(client, auth);
        var response = await ReceiveJsonAsync<WebSocketResponse>(client);

        Assert.Equal(WsMsgType.Error, response.Type);
        Assert.Equal("AUTH_FAILED", response.Code);
        Assert.False(response.Success);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    [Fact]
    public async Task Handler_QueryAfterAuth_ReturnsResults()
    {
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        // Authenticate first
        var token = _fixture.TokenService!.GenerateToken("test-user", "ws-session-2", "admin");
        await SendJsonAsync(client, new WebSocketRequest
        {
            Type = WsMsgType.Auth,
            Id = "auth-q",
            Token = token,
            Database = "testdb",
        });
        var authResponse = await ReceiveJsonAsync<WebSocketResponse>(client);
        Assert.True(authResponse.Success);

        // Setup test data
        await _fixture.ExecuteSetupSqlAsync("CREATE TABLE IF NOT EXISTS ws_test (id INT, name TEXT)");
        await _fixture.ExecuteSetupSqlAsync("INSERT INTO ws_test VALUES (1, 'Alice')");
        await _fixture.ExecuteSetupSqlAsync("INSERT INTO ws_test VALUES (2, 'Bob')");

        // Query
        await SendJsonAsync(client, new WebSocketRequest
        {
            Type = WsMsgType.Query,
            Id = "q-2",
            Sql = "SELECT * FROM ws_test",
        });

        var result = await ReceiveJsonAsync<WebSocketResponse>(client);
        Assert.Equal(WsMsgType.Result, result.Type);
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.True(result.Rows.Count >= 2);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    [Fact]
    public async Task Handler_InvalidJson_ReturnsParseError()
    {
        var handler = CreateHandler();
        var (client, server) = InProcessWebSocket.CreatePair();

        var handlerTask = handler.HandleAsync(server, CancellationToken.None);

        // Send invalid JSON
        var bytes = Encoding.UTF8.GetBytes("not-json{{{");
        await client.SendAsync(bytes.AsMemory(),
            System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);

        var response = await ReceiveJsonAsync<WebSocketResponse>(client);
        Assert.Equal(WsMsgType.Error, response.Type);
        Assert.Equal("PARSE_ERROR", response.Code);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await handlerTask;
    }

    // ── Helpers ──

    private WebSocketHandler CreateHandler()
    {
        return new WebSocketHandler(
            _fixture.DatabaseRegistry!,
            _fixture.SessionManager!,
            _fixture.TokenService!,
            new RbacService(NullLogger<RbacService>.Instance),
            Options.Create(new ServerConfiguration
            {
                ServerName = "Test",
                BindAddress = "127.0.0.1",
                GrpcPort = 0,
                DefaultDatabase = "testdb",
                Databases = [],
                Security = new SecurityConfiguration
                {
                    TlsCertificatePath = "dummy.pem",
                    TlsPrivateKeyPath = "dummy.key",
                    JwtSecretKey = "integration-test-secret-key-32chars!!",
                },
                WebSocketMaxMessageSize = 1024 * 1024,
                WebSocketKeepAliveSeconds = 30,
            }),
            NullLogger<WebSocketHandler>.Instance);
    }

    private static async Task SendJsonAsync<T>(WebSocket ws, T message)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, WebSocketJsonOptions.Default);
        await ws.SendAsync(bytes.AsMemory(),
            System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<T> ReceiveJsonAsync<T>(WebSocket ws)
    {
        var buffer = new byte[64 * 1024];
        var result = await ws.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        return JsonSerializer.Deserialize<T>(json, WebSocketJsonOptions.Default)
            ?? throw new InvalidOperationException($"Failed to deserialize: {json}");
    }
}
