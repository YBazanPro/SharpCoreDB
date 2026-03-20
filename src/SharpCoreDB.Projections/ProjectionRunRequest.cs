// <copyright file="ProjectionRunRequest.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Projection run request for a database/tenant scope.
/// </summary>
/// <param name="DatabaseId">Database identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="FromGlobalSequence">Initial global sequence to process.</param>
/// <param name="BatchSize">Maximum events to process in current run.</param>
public readonly record struct ProjectionRunRequest(
    string DatabaseId,
    string TenantId,
    long FromGlobalSequence,
    int BatchSize);
