// <copyright file="NullProjectionMetrics.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// No-op projection metrics implementation.
/// </summary>
public sealed class NullProjectionMetrics : IProjectionMetrics
{
    /// <summary>
    /// Shared singleton instance.
    /// </summary>
    public static NullProjectionMetrics Instance { get; } = new();

    private NullProjectionMetrics()
    {
    }

    /// <inheritdoc />
    public void Record(ProjectionMetricsSample sample)
    {
    }
}
