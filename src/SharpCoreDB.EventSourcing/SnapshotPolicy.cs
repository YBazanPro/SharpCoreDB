// <copyright file="SnapshotPolicy.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Snapshot policy for optional automatic snapshot creation.
/// </summary>
/// <param name="EveryNEvents">Create a snapshot whenever stream length is divisible by this value.</param>
public readonly record struct SnapshotPolicy(int EveryNEvents)
{
    /// <summary>
    /// Determines whether a snapshot should be created for the provided stream length.
    /// </summary>
    /// <param name="streamLength">Current stream length.</param>
    /// <returns><see langword="true"/> when snapshot creation should run.</returns>
    public bool ShouldCreateSnapshot(long streamLength) => EveryNEvents > 0 && streamLength > 0 && streamLength % EveryNEvents == 0;
}
