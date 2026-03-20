// <copyright file="ProjectionCheckpoint.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Represents a projection checkpoint in the global event feed.
/// </summary>
/// <param name="ProjectionName">Projection name.</param>
/// <param name="DatabaseId">Database identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="GlobalSequence">Latest successfully projected global sequence.</param>
/// <param name="UpdatedAtUtc">Checkpoint update timestamp.</param>
public readonly record struct ProjectionCheckpoint(
    string ProjectionName,
    string DatabaseId,
    string TenantId,
    long GlobalSequence,
    DateTimeOffset UpdatedAtUtc);
