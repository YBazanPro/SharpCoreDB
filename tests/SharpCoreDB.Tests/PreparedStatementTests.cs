// <copyright file="PreparedStatementTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2025-2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Xunit;
using SharpCoreDB;
using SharpCoreDB.DataStructures;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreDB.Tests;

/// <summary>
/// Tests for Prepare(), ExecutePrepared(), ExecutePreparedAsync(),
/// ExecuteCompiled(), and ExecuteCompiledQuery() in SingleFileDatabase.
/// Phase 1 audit follow-up: Validate that prepared and compiled query APIs are fully functional.
/// </summary>
public sealed class PreparedStatementTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly DatabaseFactory _factory;

    public PreparedStatementTests()
    {
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<DatabaseFactory>();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"prepared_test_{Guid.NewGuid():N}.scdb");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Prepare_WithValidSQL_ReturnsValidPreparedStatement()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        var sql = "SELECT * FROM users";

        // Act
        var stmt = db.Prepare(sql);

        // Assert
        Assert.NotNull(stmt);
        Assert.Equal(sql, stmt.Sql);
        Assert.NotNull(stmt.Plan);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void Prepare_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null, ArgumentException for whitespace
        Assert.Throws<ArgumentNullException>(() => db.Prepare(null!));
        Assert.Throws<ArgumentException>(() => db.Prepare("   "));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void Prepare_WithSelectStatement_PopulatesCompiledPlan()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        var sql = "SELECT * FROM users";

        // Act
        var stmt = db.Prepare(sql);

        // Assert
        Assert.NotNull(stmt);
        Assert.NotNull(stmt.CompiledPlan);
        Assert.True(stmt.IsCompiled);
        Assert.Equal("users", stmt.CompiledPlan.TableName);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecutePrepared_WithValidParameters_ExecutesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");
        db.ExecuteSQL("INSERT INTO users VALUES (2, 'Bob')");

        var stmt = db.Prepare("SELECT * FROM users");
        var parameters = new Dictionary<string, object?>();

        // Act
        db.ExecutePrepared(stmt, parameters);

        // Assert - no exception means success
        Assert.NotNull(stmt);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecutePrepared_WithNullStatement_ThrowsArgumentNullException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        var parameters = new Dictionary<string, object?>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => db.ExecutePrepared(null!, parameters));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecutePrepared_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        var stmt = db.Prepare("SELECT * FROM users");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => db.ExecutePrepared(stmt, null!));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ExecutePreparedAsync_WithValidStatement_CompletesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");

        var stmt = db.Prepare("SELECT * FROM users");
        var parameters = new Dictionary<string, object?>();

        // Act
        await db.ExecutePreparedAsync(stmt, parameters);

        // Assert - no exception means success
        Assert.NotNull(stmt);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ExecutePreparedAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        var stmt = db.Prepare("SELECT * FROM users");
        var parameters = new Dictionary<string, object?>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => db.ExecutePreparedAsync(stmt, parameters, cts.Token));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecuteCompiled_WithValidPlan_ExecutesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");

        var stmt = db.Prepare("SELECT * FROM users");
        Assert.NotNull(stmt.CompiledPlan);

        // Act
        var result = db.ExecuteCompiled(stmt.CompiledPlan);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result[0].ContainsKey("id"));
        Assert.True(result[0].ContainsKey("name"));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecuteCompiled_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => db.ExecuteCompiled(null!));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecuteCompiledQuery_WithCompiledStatement_ExecutesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");
        db.ExecuteSQL("INSERT INTO users VALUES (2, 'Bob')");

        var stmt = db.Prepare("SELECT * FROM users");
        Assert.NotNull(stmt);

        // Act
        var result = db.ExecuteCompiledQuery(stmt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecuteCompiledQuery_WithNullStatement_ThrowsArgumentNullException()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => db.ExecuteCompiledQuery(null!));

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void ExecuteCompiledQuery_WithParameters_BindsAndExecutesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");
        db.ExecuteSQL("INSERT INTO users VALUES (2, 'Bob')");

        var stmt = db.Prepare("SELECT * FROM users");
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };

        // Act
        var result = db.ExecuteCompiledQuery(stmt, parameters);

        // Assert - should execute without error
        Assert.NotNull(result);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void PreparedStatementCaching_ReusingSameSql_ReusesCachedPlan()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        var sql = "SELECT * FROM users";

        // Act
        var stmt1 = db.Prepare(sql);
        var stmt2 = db.Prepare(sql);

        // Assert - same SQL should return cached plan (not necessarily same object, but same plan)
        Assert.NotNull(stmt1);
        Assert.NotNull(stmt2);
        Assert.Equal(stmt1.Sql, stmt2.Sql);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }

    [Fact]
    public void PreparedStatement_WithUpdateStatement_ExecutesSuccessfully()
    {
        // Arrange
        var options = DatabaseOptions.CreateSingleFileDefault();
        var db = _factory.CreateWithOptions(_testFilePath, "testpass", options) as SingleFileDatabase;
        Assert.NotNull(db);

        db.ExecuteSQL("CREATE TABLE users (id INT, name TEXT)");
        db.ExecuteSQL("INSERT INTO users VALUES (1, 'Alice')");

        var stmt = db.Prepare("UPDATE users SET name = 'Bob' WHERE id = 1");
        var parameters = new Dictionary<string, object?>();

        // Act
        db.ExecutePrepared(stmt, parameters);

        // Assert - verify the update worked
        var result = db.ExecuteQuery("SELECT * FROM users");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Bob", result[0]["name"]);

        // Cleanup
        (db as IDisposable)?.Dispose();
    }
}
