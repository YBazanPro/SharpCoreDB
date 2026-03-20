// <copyright file="ProjectionBuilder.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

/// <summary>
/// Registers projection types for execution.
/// </summary>
public sealed class ProjectionBuilder
{
    private readonly Lock _lock = new();
    private readonly List<ProjectionDescriptor> _descriptors = [];

    /// <summary>
    /// Adds a projection type to the builder.
    /// </summary>
    /// <typeparam name="TProjection">Projection type.</typeparam>
    /// <returns>The same builder instance.</returns>
    public ProjectionBuilder AddProjection<TProjection>()
        where TProjection : IProjection
    {
        return AddProjection(typeof(TProjection));
    }

    /// <summary>
    /// Adds a projection type to the builder.
    /// </summary>
    /// <param name="projectionType">Projection type implementing <see cref="IProjection"/>.</param>
    /// <returns>The same builder instance.</returns>
    public ProjectionBuilder AddProjection(Type projectionType)
    {
        ArgumentNullException.ThrowIfNull(projectionType);

        if (!typeof(IProjection).IsAssignableFrom(projectionType))
        {
            throw new ArgumentException("Projection type must implement IProjection.", nameof(projectionType));
        }

        lock (_lock)
        {
            var descriptor = new ProjectionDescriptor(projectionType, projectionType.Name);
            if (!_descriptors.Contains(descriptor))
            {
                _descriptors.Add(descriptor);
            }
        }

        return this;
    }

    /// <summary>
    /// Builds registered projection descriptors.
    /// </summary>
    /// <returns>A snapshot of projection descriptors in registration order.</returns>
    public IReadOnlyList<ProjectionDescriptor> Build()
    {
        lock (_lock)
        {
            return [.. _descriptors];
        }
    }
}
