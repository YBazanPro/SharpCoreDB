// <copyright file="BenchmarkContext.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Benchmarks;

using System.Diagnostics;

/// <summary>
/// Base context for benchmark scenarios with common setup/teardown and metric collection.
/// </summary>
public abstract class BenchmarkContext
{
    protected string ScenarioName { get; set; } = string.Empty;
    protected List<Measurement> Measurements { get; set; } = [];
    protected Stopwatch Timer { get; set; } = new();
    protected long StartMemory { get; set; }

    /// <summary>
    /// Setup phase before benchmarking.
    /// </summary>
    public virtual void Setup()
    {
        Measurements.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        StartMemory = GC.GetTotalMemory(false) / (1024 * 1024); // MB
    }

    /// <summary>
    /// Teardown phase after benchmarking.
    /// </summary>
    public virtual void Teardown()
    {
        GC.Collect();
    }

    /// <summary>
    /// Record a single measurement with latency.
    /// </summary>
    protected void RecordMeasurement(string operation, double latencyMs, bool success = true)
    {
        Measurements.Add(new Measurement
        {
            Operation = operation,
            LatencyMs = latencyMs,
            Success = success
        });
    }

    /// <summary>
    /// Record time for an operation using a stopwatch.
    /// </summary>
    protected void RecordTiming(string operation, Action action)
    {
        Timer.Restart();
        try
        {
            action();
            Timer.Stop();
            RecordMeasurement(operation, Timer.Elapsed.TotalMilliseconds, success: true);
        }
        catch (Exception ex)
        {
            Timer.Stop();
            Console.Error.WriteLine($"Error in {operation}: {ex.Message}");
            RecordMeasurement(operation, Timer.Elapsed.TotalMilliseconds, success: false);
        }
    }

    /// <summary>
    /// Record async operation timing.
    /// </summary>
    protected async Task RecordTimingAsync(string operation, Func<Task> action)
    {
        Timer.Restart();
        try
        {
            await action();
            Timer.Stop();
            RecordMeasurement(operation, Timer.Elapsed.TotalMilliseconds, success: true);
        }
        catch (Exception ex)
        {
            Timer.Stop();
            Console.Error.WriteLine($"Error in {operation}: {ex.Message}");
            RecordMeasurement(operation, Timer.Elapsed.TotalMilliseconds, success: false);
        }
    }

    /// <summary>
    /// Generate summary statistics from measurements.
    /// </summary>
    public Summary GenerateSummary()
    {
        if (Measurements.Count == 0)
        {
            return new Summary { Count = 0 };
        }

        var latencies = Measurements.Where(m => m.Success).Select(m => m.LatencyMs).OrderBy(l => l).ToList();

        return new Summary
        {
            Count = Measurements.Count,
            ThroughputOpsSec = Measurements.Count / (latencies.Sum() / 1000),
            LatencyP50Ms = latencies.Count > 0 ? Percentile(latencies, 50) : 0,
            LatencyP99Ms = latencies.Count > 0 ? Percentile(latencies, 99) : 0,
            MemoryMB = (GC.GetTotalMemory(false) / (1024 * 1024)) - StartMemory
        };
    }

    /// <summary>
    /// Calculate percentile from sorted latency list.
    /// </summary>
    private static double Percentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;
        var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }
}
