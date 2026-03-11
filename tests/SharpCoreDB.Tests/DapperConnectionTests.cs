// <copyright file="DapperConnectionTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2025-2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.Extensions;

namespace SharpCoreDB.Tests;

/// <summary>
/// Tests the public Dapper connection surface.
/// </summary>
public sealed class DapperConnectionTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseFactory _factory;

    public DapperConnectionTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"dapper_test_{Guid.NewGuid():N}");
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<DatabaseFactory>();
    }

    [Fact]
    public void DapperConnection_ParameterCollectionCopyTo_CopiesParameters()
    {
        // Arrange
        var db = _factory.Create(_testDbPath, "testpass");
        using var connection = new DapperConnection(db, _testDbPath);
        using var command = connection.CreateCommand();
        var first = command.CreateParameter();
        first.ParameterName = "Id";
        first.Value = 1;
        var second = command.CreateParameter();
        second.ParameterName = "Name";
        second.Value = "Alice";
        command.Parameters.Add(first);
        command.Parameters.Add(second);
        var copied = new DbParameter[2];

        // Act
        command.Parameters.CopyTo(copied, 0);

        // Assert
        Assert.Same(first, copied[0]);
        Assert.Same(second, copied[1]);
    }

    [Fact]
    public void DapperConnection_ParameterCollectionCopyTo_ValidatesArguments()
    {
        // Arrange
        var db = _factory.Create(_testDbPath, "testpass");
        using var connection = new DapperConnection(db, _testDbPath);
        using var command = connection.CreateCommand();
        var param = command.CreateParameter();
        param.ParameterName = "Id";
        param.Value = 1;
        command.Parameters.Add(param);

        // Act & Assert - null array
        Assert.Throws<ArgumentNullException>(() => command.Parameters.CopyTo(null!, 0));

        // Act & Assert - multidimensional array
        Array multiArray = Array.CreateInstance(typeof(DbParameter), [2, 2]);
        Assert.Throws<ArgumentException>(() => command.Parameters.CopyTo(multiArray, 0));

        // Act & Assert - negative index
        var array = new DbParameter[2];
        Assert.Throws<ArgumentOutOfRangeException>(() => command.Parameters.CopyTo(array, -1));

        // Act & Assert - insufficient space (1 param, array length 2, index 2 → 0 slots left)
        Assert.Throws<ArgumentException>(() => command.Parameters.CopyTo(array, 2));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }
}
