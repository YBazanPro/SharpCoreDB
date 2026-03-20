// <copyright file="ProjectionExecutionContext.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Projection execution context for multi-database and multi-tenant processing.
/// </summary>
/// <param name="DatabaseId">Logical database identifier.</param>
/// <param name="TenantId">Logical tenant identifier.</param>
public readonly record struct ProjectionExecutionContext(
    string DatabaseId,
    string TenantId);
