// <copyright file="ProjectionRunResult.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Projection run result.
/// </summary>
/// <param name="ProcessedEvents">Number of projected events.</param>
/// <param name="LastGlobalSequence">Last processed global sequence.</param>
public readonly record struct ProjectionRunResult(
    int ProcessedEvents,
    long LastGlobalSequence);
