// <copyright file="OutboxRetryPolicyOptions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Configurable retry policy for CQRS outbox failure handling.
/// </summary>
public sealed class OutboxRetryPolicyOptions
{
    /// <summary>
    /// Gets or sets the initial retry delay for first failure.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum retry delay cap.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before dead-lettering.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the dead-letter table name used by persistent outbox.
    /// </summary>
    public string DeadLetterTableName { get; set; } = "scdb_outbox_deadletter";
}
