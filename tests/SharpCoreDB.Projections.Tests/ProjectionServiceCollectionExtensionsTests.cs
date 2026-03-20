// <copyright file="ProjectionServiceCollectionExtensionsTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for projection dependency injection extensions.
/// </summary>
public class ProjectionServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharpCoreDBProjections_WithDefaults_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddSharpCoreDBProjections();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<InlineProjectionRunner>(provider.GetRequiredService<IProjectionRunner>());
    }

    [Fact]
    public void AddProjection_WithProjectionType_RegistersProjectionService()
    {
        var services = new ServiceCollection();

        services.AddProjection<TestProjection>();

        using var provider = services.BuildServiceProvider();
        Assert.Contains(provider.GetServices<IProjection>(), static projection => projection is TestProjection);
    }

    [Fact]
    public void AddSharpCoreDBProjections_WithDefaults_RegistersMetricsService()
    {
        var services = new ServiceCollection();

        services.AddSharpCoreDBProjections();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<InMemoryProjectionMetrics>(provider.GetRequiredService<IProjectionMetrics>());
    }

    [Fact]
    public void UseOpenTelemetryProjectionMetrics_WithDefaults_ReplacesMetricsService()
    {
        var services = new ServiceCollection();

        services.AddSharpCoreDBProjections();
        services.UseOpenTelemetryProjectionMetrics();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<OpenTelemetryProjectionMetrics>(provider.GetRequiredService<IProjectionMetrics>());
    }

    private sealed class TestProjection : IProjection
    {
        public string Name => nameof(TestProjection);

        public Task ProjectAsync(
            EventEnvelope envelope,
            ProjectionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
