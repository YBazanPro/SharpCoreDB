// <copyright file="QueryCompilerSafetyTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2025-2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using SharpCoreDB.Services;
using Xunit;

namespace SharpCoreDB.Tests;

/// <summary>
/// Safety tests for compiled query fallback behavior.
/// </summary>
public sealed class QueryCompilerSafetyTests
{
    [Fact]
    public void Compile_WithUnsupportedWhereShape_ReturnsNullPlan()
    {
        // Arrange
        var sql = "SELECT id FROM users WHERE id IN (1, 2, 3)";

        // Act
        var plan = QueryCompiler.Compile(sql);

        // Assert
        Assert.Null(plan);
    }
}
