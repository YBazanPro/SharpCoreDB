// <copyright file="ProjectionExecutionMode.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Projection execution strategy.
/// </summary>
public enum ProjectionExecutionMode
{
    /// <summary>
    /// Execute projections inline with event processing.
    /// </summary>
    Inline = 0,

    /// <summary>
    /// Execute projections in a background catch-up worker.
    /// </summary>
    Background = 1,
}
