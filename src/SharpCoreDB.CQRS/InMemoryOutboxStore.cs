// <copyright file="InMemoryOutboxStore.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.CQRS;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe in-memory outbox store.
/// </summary>
public sealed class InMemoryOutboxStore(OutboxRetryPolicyOptions? retryPolicy = null) : IOutboxStore
{
    private readonly OutboxRetryPolicyOptions _retryPolicy = retryPolicy ?? new OutboxRetryPolicyOptions();
    private readonly ConcurrentDictionary<string, OutboxMessage> _messages = [];
    private readonly ConcurrentDictionary<string, OutboxMessage> _deadLetters = [];
    private readonly ConcurrentDictionary<string, FailureState> _failures = [];

    /// <inheritdoc />
    public Task<bool> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(message.MessageId);

        if (_messages.ContainsKey(message.MessageId) || _deadLetters.ContainsKey(message.MessageId))
        {
            return Task.FromResult(false);
        }

        _messages[message.MessageId] = message;
        _deadLetters.TryRemove(message.MessageId, out _);
        _failures.TryRemove(message.MessageId, out _);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var messages = _messages.Values
            .Where(static message => !message.IsPublished)
            .Where(message => !_failures.TryGetValue(message.MessageId, out var failure) || failure.NextAttemptUtc <= nowUtc)
            .OrderBy(static message => message.CreatedAtUtc)
            .Take(limit)
            .ToArray();

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(messages);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        }

        var messages = _deadLetters.Values
            .OrderBy(static message => message.CreatedAtUtc)
            .Take(limit)
            .ToArray();

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(messages);
    }

    /// <inheritdoc />
    public Task MarkPublishedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (_messages.TryGetValue(messageId, out var message))
        {
            _messages[messageId] = message with { IsPublished = true };
            _deadLetters.TryRemove(messageId, out _);
            _failures.TryRemove(messageId, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordFailureAsync(string messageId, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        var failureState = _failures.AddOrUpdate(
            messageId,
            _ =>
            {
                var attempts = 1;
                return new FailureState(attempts, DateTimeOffset.UtcNow.Add(GetBackoff(attempts)));
            },
            (_, existing) =>
            {
                var attempts = existing.AttemptCount + 1;
                return new FailureState(attempts, DateTimeOffset.UtcNow.Add(GetBackoff(attempts)));
            });

        if (failureState.AttemptCount >= _retryPolicy.MaxAttempts && _messages.TryRemove(messageId, out var message))
        {
            _deadLetters[messageId] = message;
            _failures.TryRemove(messageId, out _);
        }

        return Task.CompletedTask;
    }

    private TimeSpan GetBackoff(int attemptCount)
    {
        var boundedAttempt = Math.Clamp(attemptCount, 1, 30);
        var seconds = _retryPolicy.BaseDelay.TotalSeconds * Math.Pow(2, boundedAttempt - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, _retryPolicy.MaxDelay.TotalSeconds));
    }

    /// <inheritdoc />
    public Task RequeueDeadLetterAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (_deadLetters.TryRemove(messageId, out var message))
        {
            _messages[messageId] = message;
            _failures.TryRemove(messageId, out _);
        }

        return Task.CompletedTask;
    }

    private readonly record struct FailureState(int AttemptCount, DateTimeOffset NextAttemptUtc);
}
