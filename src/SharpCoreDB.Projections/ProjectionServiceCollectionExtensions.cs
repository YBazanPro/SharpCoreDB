// <copyright file="ProjectionServiceCollectionExtensions.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharpCoreDB.Interfaces;

/// <summary>
/// Dependency injection extensions for SharpCoreDB projection services.
/// </summary>
public static class ProjectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers core projection services with in-memory checkpoints.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional projection engine options configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSharpCoreDBProjections(
        this IServiceCollection services,
        Action<ProjectionEngineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ProjectionEngineOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IProjectionMetrics, InMemoryProjectionMetrics>();
        services.TryAddSingleton<IProjectionCheckpointStore, InMemoryProjectionCheckpointStore>();
        services.TryAddSingleton<IProjectionRunner, InlineProjectionRunner>();

        return services;
    }

    /// <summary>
    /// Registers a projection implementation for dependency injection.
    /// </summary>
    /// <typeparam name="TProjection">Projection type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjection<TProjection>(this IServiceCollection services)
        where TProjection : class, IProjection
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProjection, TProjection>());
        return services;
    }

    /// <summary>
    /// Replaces in-memory checkpoint storage with persistent SharpCoreDB-backed checkpoints.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="tableName">Optional checkpoint table name.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection UseSharpCoreDBProjectionCheckpoints(
        this IServiceCollection services,
        string tableName = "scdb_projection_checkpoints")
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<IProjectionCheckpointStore>(sp =>
        {
            var database = sp.GetRequiredService<IDatabase>();
            return new SharpCoreDbProjectionCheckpointStore(database, tableName);
        }));

        return services;
    }

    /// <summary>
    /// Registers hosted background projection worker service.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional hosted worker options configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSharpCoreDBProjectionHostedWorker(
        this IServiceCollection services,
        Action<ProjectionHostedServiceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ProjectionHostedServiceOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.AddHostedService<ProjectionBackgroundHostedService>();
        return services;
    }

    /// <summary>
    /// Replaces default projection metrics with OpenTelemetry-backed metrics.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="meterName">Optional meter name.</param>
    /// <param name="instrumentationVersion">Optional instrumentation version.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection UseOpenTelemetryProjectionMetrics(
        this IServiceCollection services,
        string meterName = OpenTelemetryProjectionMetrics.MeterName,
        string instrumentationVersion = OpenTelemetryProjectionMetrics.InstrumentationVersion)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<IProjectionMetrics>(
            _ => new OpenTelemetryProjectionMetrics(meterName, instrumentationVersion)));

        return services;
    }
}
