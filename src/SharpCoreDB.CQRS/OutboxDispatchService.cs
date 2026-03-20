// <copyright file="OutboxDispatchService.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using Microsoft.Extensions.Logging;

/// <summary>
/// Dispatches unpublished outbox messages using a configured publisher.
/// </summary>
public sealed class OutboxDispatchService(
    IOutboxStore outboxStore,
    IOutboxPublisher outboxPublisher,
    ILogger<OutboxDispatchService>? logger = null)
{
    private readonly IOutboxStore _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
    private readonly IOutboxPublisher _outboxPublisher = outboxPublisher ?? throw new ArgumentNullException(nameof(outboxPublisher));
    private readonly ILogger<OutboxDispatchService>? _logger = logger;

    /// <summary>
    /// Publishes and marks unpublished outbox messages.
    /// </summary>
    /// <param name="limit">Maximum messages to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of successfully published messages.</returns>
    public async Task<int> DispatchUnpublishedAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            return 0;
        }

        var messages = await _outboxStore.GetUnpublishedAsync(limit, cancellationToken).ConfigureAwait(false);
        var publishedCount = 0;
        var failedCount = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _outboxPublisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
                await _outboxStore.MarkPublishedAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
                publishedCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _outboxStore.RecordFailureAsync(message.MessageId, ex.Message, cancellationToken).ConfigureAwait(false);
                failedCount++;
                _logger?.LogWarning(ex, "Outbox message {MessageId} ({MessageType}) failed to publish.", message.MessageId, message.MessageType);
            }
        }

        if (publishedCount > 0 || failedCount > 0)
        {
            var deadLetters = await _outboxStore.GetDeadLettersAsync(int.MaxValue, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("Outbox dispatch completed. Published: {Published}, Failed: {Failed}, Total: {Total}, DeadLetters: {DeadLetters}.",
                publishedCount, failedCount, messages.Count, deadLetters.Count);
        }

        return publishedCount;
    }
}
