// <copyright file="ProjectionBuilderTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="ProjectionBuilder"/>.
/// </summary>
public class ProjectionBuilderTests
{
    [Fact]
    public void AddProjection_WithProjectionType_RegistersDescriptor()
    {
        var builder = new ProjectionBuilder();

        builder.AddProjection<TestProjection>();

        var descriptors = builder.Build();
        Assert.Contains(descriptors, descriptor => descriptor.Name == nameof(TestProjection));
    }

    private sealed class TestProjection : IProjection
    {
        public string Name => nameof(TestProjection);

        public Task ProjectAsync(EventEnvelope envelope, ProjectionExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
