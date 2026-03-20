// <copyright file="OutboxDispatchBackgroundService.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hosted background service that periodically dispatches unpublished outbox messages.
/// </summary>
public class OutboxDispatchBackgroundService(
    OutboxDispatchService dispatchService,
    OutboxHostedServiceOptions options,
    ILogger<OutboxDispatchBackgroundService>? logger = null) : BackgroundService
{
    private readonly OutboxDispatchService _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
    private readonly OutboxHostedServiceOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<OutboxDispatchBackgroundService>? _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.BatchSize <= 0)
        {
            throw new InvalidOperationException("OutboxHostedServiceOptions.BatchSize must be greater than zero.");
        }

        if (_options.PollInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("OutboxHostedServiceOptions.PollInterval must be greater than zero.");
        }

        if (_options.MaxIterations is <= 0)
        {
            throw new InvalidOperationException("OutboxHostedServiceOptions.MaxIterations must be greater than zero when specified.");
        }

        _logger?.LogInformation("Outbox background service started. BatchSize: {BatchSize}, PollInterval: {PollInterval}, MaxIterations: {MaxIterations}.",
            _options.BatchSize, _options.PollInterval, _options.MaxIterations);

        var iteration = 0;
        var totalPublished = 0;
        var totalFailed = 0;
        var startTime = DateTimeOffset.UtcNow;

        using var timer = new PeriodicTimer(_options.PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var published = await _dispatchService.DispatchUnpublishedAsync(_options.BatchSize, stoppingToken).ConfigureAwait(false);

                if (published > 0)
                {
                    totalPublished += published;
                    _logger?.LogDebug("Outbox: dispatched {Count} message(s).", published);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                _logger?.LogError(ex, "Outbox dispatch cycle failed. Will retry on next tick.");
            }

            iteration++;

            // Log periodic health summary every 10 iterations or when significant activity
            if (iteration % 10 == 0 || totalPublished > 0)
            {
                var elapsed = DateTimeOffset.UtcNow - startTime;
                _logger?.LogInformation("Outbox health summary. Iterations: {Iterations}, TotalPublished: {TotalPublished}, TotalFailed: {TotalFailed}, Elapsed: {Elapsed}.",
                    iteration, totalPublished, totalFailed, elapsed);
            }

            if (_options.MaxIterations is { } maxIterations && iteration >= maxIterations)
            {
                break;
            }
        }

        var finalElapsed = DateTimeOffset.UtcNow - startTime;
        _logger?.LogInformation("Outbox background service stopped. Final stats - Iterations: {Iterations}, TotalPublished: {TotalPublished}, TotalFailed: {TotalFailed}, Elapsed: {Elapsed}.",
            iteration, totalPublished, totalFailed, finalElapsed);
    }
}
