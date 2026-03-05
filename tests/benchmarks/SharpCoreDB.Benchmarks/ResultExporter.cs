// <copyright file="ResultExporter.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Benchmarks;

using System.Text;
using System.Text.Json;

/// <summary>
/// Exports benchmark results to JSON and CSV formats.
/// </summary>
public static class ResultExporter
{
    /// <summary>
    /// Exports benchmark results to JSON file.
    /// </summary>
    public static void ExportToJSON(BenchmarkResults results, string outputPath)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        
        var json = JsonSerializer.Serialize(results, options);
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"[Export] JSON results saved to: {outputPath}");
    }

    /// <summary>
    /// Exports benchmark results to CSV file.
    /// </summary>
    public static void ExportToCSV(BenchmarkResults results, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        
        // Export summary CSV
        var summaryCsvPath = Path.Combine(outputDir, "benchmark-summary.csv");
        ExportSummaryCSV(results, summaryCsvPath);
        
        // Export detailed CSVs per scenario
        foreach (var scenario in results.Scenarios)
        {
            var scenarioCsvPath = Path.Combine(outputDir, $"{SanitizeFileName(scenario.Name)}-details.csv");
            ExportScenarioDetailsCSV(scenario, scenarioCsvPath);
        }
        
        Console.WriteLine($"[Export] CSV files saved to: {outputDir}");
    }

    private static void ExportSummaryCSV(BenchmarkResults results, string outputPath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Scenario,Operation,Count,Throughput (ops/sec),Latency p50 (ms),Latency p99 (ms),Memory (MB)");
        
        foreach (var scenario in results.Scenarios)
        {
            csv.AppendLine($"{scenario.Name},Total,{scenario.Summary.Count},{scenario.Summary.ThroughputOpsSec:F2},{scenario.Summary.LatencyP50Ms:F3},{scenario.Summary.LatencyP99Ms:F3},{scenario.Summary.MemoryMB:F2}");
        }
        
        File.WriteAllText(outputPath, csv.ToString());
        Console.WriteLine($"[Export] Summary CSV: {outputPath}");
    }

    private static void ExportScenarioDetailsCSV(ScenarioResult scenario, string outputPath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Operation,Latency (ms),Success");
        
        foreach (var measurement in scenario.Measurements)
        {
            csv.AppendLine($"{measurement.Operation},{measurement.LatencyMs:F6},{measurement.Success}");
        }
        
        File.WriteAllText(outputPath, csv.ToString());
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return sanitized.Replace(" ", "-").ToLowerInvariant();
    }

    /// <summary>
    /// Exports latency distribution histogram to CSV.
    /// </summary>
    public static void ExportLatencyHistogram(string scenarioName, List<double> latencies, string outputPath, int buckets = 20)
    {
        if (latencies.Count == 0) return;

        var min = latencies.Min();
        var max = latencies.Max();
        var bucketSize = (max - min) / buckets;
        
        var histogram = new int[buckets];
        foreach (var latency in latencies)
        {
            var bucketIndex = (int)((latency - min) / bucketSize);
            bucketIndex = Math.Min(bucketIndex, buckets - 1);
            histogram[bucketIndex]++;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Bucket Start (ms),Bucket End (ms),Count,Percentage");
        
        for (int i = 0; i < buckets; i++)
        {
            var bucketStart = min + (i * bucketSize);
            var bucketEnd = min + ((i + 1) * bucketSize);
            var percentage = (histogram[i] / (double)latencies.Count) * 100;
            
            csv.AppendLine($"{bucketStart:F3},{bucketEnd:F3},{histogram[i]},{percentage:F2}");
        }
        
        File.WriteAllText(outputPath, csv.ToString());
        Console.WriteLine($"[Export] Histogram CSV for {scenarioName}: {outputPath}");
    }

    /// <summary>
    /// Exports percentile analysis to CSV.
    /// </summary>
    public static void ExportPercentiles(string scenarioName, List<double> latencies, string outputPath)
    {
        if (latencies.Count == 0) return;

        var percentiles = new[] { 50, 75, 90, 95, 99, 99.9, 99.99 };
        
        var csv = new StringBuilder();
        csv.AppendLine("Percentile,Latency (ms)");
        
        foreach (var p in percentiles)
        {
            var value = CalculatePercentile(latencies, p);
            csv.AppendLine($"p{p},{value:F3}");
        }
        
        // Add min/max
        csv.AppendLine($"Min,{latencies.Min():F3}");
        csv.AppendLine($"Max,{latencies.Max():F3}");
        csv.AppendLine($"Mean,{latencies.Average():F3}");
        
        File.WriteAllText(outputPath, csv.ToString());
        Console.WriteLine($"[Export] Percentiles CSV for {scenarioName}: {outputPath}");
    }

    private static double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        
        var sorted = new List<double>(values);
        sorted.Sort();
        
        var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));
        
        return sorted[index];
    }

    /// <summary>
    /// Creates a markdown report from benchmark results.
    /// </summary>
    public static void GenerateMarkdownReport(BenchmarkResults results, string outputPath)
    {
        var md = new StringBuilder();
        md.AppendLine("# SharpCoreDB Benchmark Report");
        md.AppendLine();
        md.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"**Run Date:** {results.Metadata.RunDate:yyyy-MM-dd HH:mm:ss} UTC");
        md.AppendLine();
        
        // Environment
        md.AppendLine("## Environment");
        md.AppendLine();
        md.AppendLine($"- **Runtime:** {results.Metadata.Runtime.DotnetVersion}");
        md.AppendLine($"- **Architecture:** {results.Metadata.Runtime.Architecture}");
        md.AppendLine($"- **CPU:** {results.Metadata.Hardware.CPU} ({results.Metadata.Hardware.Cores} cores)");
        md.AppendLine($"- **Memory:** {results.Metadata.Hardware.MemoryGB} GB");
        md.AppendLine($"- **OS:** {results.Metadata.Environment.Name} (Build {results.Metadata.Environment.Build})");
        md.AppendLine();
        
        // Scenarios
        md.AppendLine("## Benchmark Results");
        md.AppendLine();
        
        foreach (var scenario in results.Scenarios)
        {
            md.AppendLine($"### {scenario.Name}");
            md.AppendLine();
            md.AppendLine($"| Metric | Value |");
            md.AppendLine($"|--------|-------|");
            md.AppendLine($"| Total Operations | {scenario.Summary.Count:N0} |");
            md.AppendLine($"| Throughput | {scenario.Summary.ThroughputOpsSec:F0} ops/sec |");
            md.AppendLine($"| Latency p50 | {scenario.Summary.LatencyP50Ms:F3} ms |");
            md.AppendLine($"| Latency p99 | {scenario.Summary.LatencyP99Ms:F3} ms |");
            md.AppendLine($"| Memory Usage | {scenario.Summary.MemoryMB:F2} MB |");
            md.AppendLine();
        }
        
        File.WriteAllText(outputPath, md.ToString());
        Console.WriteLine($"[Export] Markdown report: {outputPath}");
    }
}
