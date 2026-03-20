// <copyright file="ProjectionHostedWorkerRegistrationTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for projection hosted worker registration.
/// </summary>
public class ProjectionHostedWorkerRegistrationTests
{
    [Fact]
    public async Task AddSharpCoreDBProjectionHostedWorker_WithBoundedOptions_StartsAndStopsHostedService()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        services.AddSingleton<IEventStore>(new InMemoryEventStore());
        services.AddProjection<NoOpProjection>();
        services.AddSharpCoreDBProjections(options =>
        {
            options.BatchSize = 10;
            options.PollInterval = TimeSpan.FromMilliseconds(5);
            options.RunOnStart = true;
            options.MaxIterations = 1;
        });
        services.AddSharpCoreDBProjectionHostedWorker(options =>
        {
            options.DatabaseId = "main";
            options.TenantId = "tenant-a";
            options.FromGlobalSequence = 1;
        });

        using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single(static service => service is ProjectionBackgroundHostedService);

        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);

        Assert.IsType<ProjectionBackgroundHostedService>(hostedService);
    }

    [Fact]
    public async Task AddSharpCoreDBProjectionHostedWorker_WithoutProjections_StartsAndStopsHostedService()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        services.AddSingleton<IEventStore>(new InMemoryEventStore());
        services.AddSharpCoreDBProjections(options =>
        {
            options.BatchSize = 10;
            options.PollInterval = TimeSpan.FromMilliseconds(5);
            options.RunOnStart = true;
            options.MaxIterations = 1;
        });
        services.AddSharpCoreDBProjectionHostedWorker(options =>
        {
            options.DatabaseId = "main";
            options.TenantId = "tenant-a";
            options.FromGlobalSequence = 1;
        });

        using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single(static service => service is ProjectionBackgroundHostedService);

        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);

        Assert.IsType<ProjectionBackgroundHostedService>(hostedService);
    }

    private sealed class NoOpProjection : IProjection
    {
        public string Name => nameof(NoOpProjection);

        public Task ProjectAsync(
            EventEnvelope envelope,
            ProjectionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
