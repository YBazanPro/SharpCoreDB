// <copyright file="ProjectionBackgroundHostedService.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpCoreDB.EventSourcing;

/// <summary>
/// Hosted service hook that executes projection catch-up in the background.
/// </summary>
public sealed class ProjectionBackgroundHostedService(
    IEventStore eventStore,
    IEnumerable<IProjection> projections,
    IProjectionRunner projectionRunner,
    ProjectionEngineOptions projectionEngineOptions,
    ProjectionHostedServiceOptions hostedServiceOptions,
    ILogger<ProjectionBackgroundHostedService>? logger = null) : BackgroundService
{
    private readonly IEventStore _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    private readonly IReadOnlyList<IProjection> _projections = [.. projections ?? throw new ArgumentNullException(nameof(projections))];
    private readonly IProjectionRunner _projectionRunner = projectionRunner ?? throw new ArgumentNullException(nameof(projectionRunner));
    private readonly ProjectionEngineOptions _projectionEngineOptions = projectionEngineOptions ?? throw new ArgumentNullException(nameof(projectionEngineOptions));
    private readonly ProjectionHostedServiceOptions _hostedServiceOptions = hostedServiceOptions ?? throw new ArgumentNullException(nameof(hostedServiceOptions));
    private readonly ILogger<ProjectionBackgroundHostedService> _logger = logger ?? NullLogger<ProjectionBackgroundHostedService>.Instance;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_hostedServiceOptions.DatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(_hostedServiceOptions.TenantId);

        if (_projections.Count == 0)
        {
            _logger.LogInformation("Projection hosted worker is enabled but no projections were registered. Worker will remain idle.");
            return;
        }

        var worker = new BackgroundProjectionWorker(_projectionRunner, _projectionEngineOptions);
        await worker.RunAsync(
            _eventStore,
            _projections,
            _hostedServiceOptions.DatabaseId,
            _hostedServiceOptions.TenantId,
            _hostedServiceOptions.FromGlobalSequence,
            stoppingToken).ConfigureAwait(false);
    }
}
