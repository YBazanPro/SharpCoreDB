// <copyright file="BackgroundProjectionWorkerTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Projections.Tests;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Unit tests for <see cref="BackgroundProjectionWorker"/>.
/// </summary>
public class BackgroundProjectionWorkerTests
{
    [Fact]
    public async Task RunAsync_WithRunOnStartAndMaxIterations_ProcessesExpectedCycles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var fakeRunner = new FakeProjectionRunner(processedEventsPerRun: 3);
        var worker = new BackgroundProjectionWorker(
            fakeRunner,
            new ProjectionEngineOptions
            {
                BatchSize = 50,
                PollInterval = TimeSpan.FromMilliseconds(5),
                RunOnStart = true,
                MaxIterations = 2,
            });

        var processed = await worker.RunAsync(
            new InMemoryEventStore(),
            [new NoOpProjection()],
            "main",
            "tenant-a",
            fromGlobalSequence: 1,
            cancellationToken);

        Assert.Equal(6, processed);
    }

    [Fact]
    public async Task RunAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var worker = new BackgroundProjectionWorker(
            new FakeProjectionRunner(processedEventsPerRun: 1),
            new ProjectionEngineOptions
            {
                BatchSize = 50,
                PollInterval = TimeSpan.FromMilliseconds(5),
                RunOnStart = true,
                MaxIterations = 1,
            });

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await worker.RunAsync(
                new InMemoryEventStore(),
                [new NoOpProjection()],
                "main",
                "tenant-a",
                fromGlobalSequence: 1,
                cts.Token));
    }

    private sealed class FakeProjectionRunner(int processedEventsPerRun) : IProjectionRunner
    {
        private readonly int _processedEventsPerRun = processedEventsPerRun;

        public Task<ProjectionRunResult> RunAsync(
            IEventStore eventStore,
            IProjection projection,
            ProjectionRunRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ProjectionRunResult(_processedEventsPerRun, request.FromGlobalSequence + _processedEventsPerRun));
        }
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
