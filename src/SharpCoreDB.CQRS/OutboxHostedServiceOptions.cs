// <copyright file="OutboxHostedServiceOptions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Options for the hosted outbox dispatch background worker.
/// </summary>
public sealed class OutboxHostedServiceOptions
{
    /// <summary>
    /// Gets or sets how many unpublished messages to dispatch per polling cycle.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the polling interval between dispatch cycles.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of dispatch iterations.
    /// When <see langword="null"/> the worker runs indefinitely until cancellation.
    /// </summary>
    public int? MaxIterations { get; set; }
}
