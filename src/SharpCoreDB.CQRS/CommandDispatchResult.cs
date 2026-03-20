// <copyright file="CommandDispatchResult.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

/// <summary>
/// Result of command dispatch and handling.
/// </summary>
/// <param name="Success">Whether command handling succeeded.</param>
/// <param name="Message">Optional result message.</param>
public readonly record struct CommandDispatchResult(
    bool Success,
    string? Message)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <returns>Successful dispatch result.</returns>
    public static CommandDispatchResult Ok(string? message = null) => new(true, message);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">Failure reason.</param>
    /// <returns>Failed dispatch result.</returns>
    public static CommandDispatchResult Fail(string message) => new(false, message);
}
