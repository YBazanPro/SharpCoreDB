// <copyright file="IProjectionMetrics.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Provides projection metrics reporting hooks.
/// </summary>
public interface IProjectionMetrics
{
    /// <summary>
    /// Records a projection run metrics sample.
    /// </summary>
    /// <param name="sample">Projection run metrics sample.</param>
    void Record(ProjectionMetricsSample sample);
}
