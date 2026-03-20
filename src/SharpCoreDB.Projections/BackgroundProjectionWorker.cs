// <copyright file="BackgroundProjectionWorker.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Background projection worker that executes projections on a polling cadence.
/// </summary>
public sealed class BackgroundProjectionWorker(
    IProjectionRunner projectionRunner,
    ProjectionEngineOptions options)
{
    private readonly IProjectionRunner _projectionRunner = projectionRunner ?? throw new ArgumentNullException(nameof(projectionRunner));
    private readonly ProjectionEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Runs projection processing until cancellation or configured iteration limit.
    /// </summary>
    /// <param name="eventStore">Event store source.</param>
    /// <param name="projections">Projection instances to run in sequence.</param>
    /// <param name="databaseId">Database identifier.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="fromGlobalSequence">Initial global sequence when no checkpoint exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of processed events across all projections.</returns>
    public async Task<long> RunAsync(
        IEventStore eventStore,
        IReadOnlyList<IProjection> projections,
        string databaseId,
        string tenantId,
        long fromGlobalSequence = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (projections.Count == 0)
        {
            throw new ArgumentException("At least one projection must be provided.", nameof(projections));
        }

        if (_options.BatchSize <= 0)
        {
            throw new InvalidOperationException("ProjectionEngineOptions.BatchSize must be greater than zero.");
        }

        if (_options.PollInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("ProjectionEngineOptions.PollInterval must be greater than zero.");
        }

        if (_options.MaxIterations is <= 0)
        {
            throw new InvalidOperationException("ProjectionEngineOptions.MaxIterations must be greater than zero when specified.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var processedEvents = 0L;
        var iteration = 0;

        if (_options.RunOnStart)
        {
            processedEvents += await RunCycleAsync(eventStore, projections, databaseId, tenantId, fromGlobalSequence, cancellationToken).ConfigureAwait(false);
            iteration++;

            if (_options.MaxIterations is { } maxIterations && iteration >= maxIterations)
            {
                return processedEvents;
            }
        }

        using var timer = new PeriodicTimer(_options.PollInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            processedEvents += await RunCycleAsync(eventStore, projections, databaseId, tenantId, fromGlobalSequence, cancellationToken).ConfigureAwait(false);
            iteration++;

            if (_options.MaxIterations is { } maxIterations && iteration >= maxIterations)
            {
                break;
            }
        }

        return processedEvents;
    }

    private async Task<long> RunCycleAsync(
        IEventStore eventStore,
        IReadOnlyList<IProjection> projections,
        string databaseId,
        string tenantId,
        long fromGlobalSequence,
        CancellationToken cancellationToken)
    {
        var cycleProcessed = 0L;

        foreach (var projection in projections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var runResult = await _projectionRunner.RunAsync(
                eventStore,
                projection,
                new ProjectionRunRequest(databaseId, tenantId, fromGlobalSequence, _options.BatchSize),
                cancellationToken).ConfigureAwait(false);

            cycleProcessed += runResult.ProcessedEvents;
        }

        return cycleProcessed;
    }
}
