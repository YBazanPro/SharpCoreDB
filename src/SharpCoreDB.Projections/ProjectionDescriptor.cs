// <copyright file="ProjectionDescriptor.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Describes a registered projection type.
/// </summary>
/// <param name="ProjectionType">Projection implementation type.</param>
/// <param name="Name">Stable projection name.</param>
public readonly record struct ProjectionDescriptor(
    Type ProjectionType,
    string Name);
