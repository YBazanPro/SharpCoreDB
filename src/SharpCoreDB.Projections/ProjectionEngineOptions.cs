// <copyright file="ProjectionEngineOptions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Projection engine configuration options.
/// </summary>
public sealed class ProjectionEngineOptions
{
    /// <summary>
    /// Gets or sets projection execution mode.
    /// </summary>
    public ProjectionExecutionMode ExecutionMode { get; set; } = ProjectionExecutionMode.Background;

    /// <summary>
    /// Gets or sets maximum events read per batch.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets polling interval used by background execution.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets or sets a value indicating whether the background worker should execute immediately before waiting for the first timer tick.
    /// </summary>
    public bool RunOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets maximum loop iterations for background execution. Use <see langword="null"/> for continuous execution until cancellation.
    /// </summary>
    public int? MaxIterations { get; set; }
}
