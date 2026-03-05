// <copyright file="EventStreamId.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Represents a logical event stream identifier.
/// </summary>
public readonly record struct EventStreamId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamId"/> struct.
    /// </summary>
    /// <param name="value">The stream identifier text.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
    public EventStreamId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or white space.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the stream identifier text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the stream identifier text.
    /// </summary>
    public override string ToString() => Value;
}
