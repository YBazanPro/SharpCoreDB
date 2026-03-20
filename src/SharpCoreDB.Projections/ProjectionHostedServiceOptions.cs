// <copyright file="ProjectionHostedServiceOptions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Options for hosted background projection execution.
/// </summary>
public sealed class ProjectionHostedServiceOptions
{
    /// <summary>
    /// Gets or sets logical database identifier for projection execution scope.
    /// </summary>
    public string DatabaseId { get; set; } = "main";

    /// <summary>
    /// Gets or sets logical tenant identifier for projection execution scope.
    /// </summary>
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// Gets or sets initial global sequence when no checkpoint exists.
    /// </summary>
    public long FromGlobalSequence { get; set; } = 1;
}
