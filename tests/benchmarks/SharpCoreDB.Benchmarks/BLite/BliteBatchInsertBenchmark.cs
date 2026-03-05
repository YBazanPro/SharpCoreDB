// <copyright file="BliteBatchInsertBenchmark.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Benchmarks.BLite;

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Scenario B2: Batch Insert (1M Documents).
/// Measures bulk load performance and memory efficiency with varying batch sizes.
/// </summary>
public class BliteBatchInsertBenchmark : BenchmarkContext
{
    private Database? _db;
    private string _dbPath = string.Empty;
    private const int TotalDocuments = 1_000_000;
    private readonly int[] _batchSizes = [1_000, 5_000, 10_000, 50_000];
    private const string TableName = "documents";
    
    private class BatchResult
    {
        public int BatchSize { get; set; }
        public List<double> BatchLatencies { get; set; } = [];
        public long MemoryAt100K { get; set; }
        public long MemoryAt500K { get; set; }
        public long MemoryAt1M { get; set; }
        public double TotalTime { get; set; }
        public double Throughput { get; set; }
    }

    private readonly Dictionary<int, BatchResult> _results = [];

    public override void Setup()
    {
        base.Setup();
        ScenarioName = "BLite B2: Batch Insert (1M documents)";
        Console.WriteLine($"[B2] Setup: {ScenarioName}");
        Console.WriteLine($"[B2] Testing batch sizes: {string.Join(", ", _batchSizes)}");
    }

    public async Task Run()
    {
        Console.WriteLine($"[B2] Running: {ScenarioName}");
        Console.WriteLine();

        foreach (var batchSize in _batchSizes)
        {
            Console.WriteLine($"[B2] ========================================");
            Console.WriteLine($"[B2] Testing Batch Size: {batchSize:N0}");
            Console.WriteLine($"[B2] ========================================");

            await RunBatchSizeTest(batchSize);
            
            Console.WriteLine();
        }

        // Print summary
        PrintSummary();
    }

    private async Task RunBatchSizeTest(int batchSize)
    {
        // Create fresh database for this test
        _dbPath = Path.Combine(Path.GetTempPath(), $"blite-b2-batch{batchSize}-{Guid.NewGuid()}");
        
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        var serviceProvider = services.BuildServiceProvider();
        
        _db = new Database(
            services: serviceProvider,
            dbPath: _dbPath,
            masterPassword: "benchmark-password-123",
            isReadOnly: false
        );

        // Create schema
        var createTableSQL = $@"
            CREATE TABLE {TableName} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT UNIQUE,
                age INTEGER,
                score REAL,
                tags TEXT,
                created_at DATETIME,
                updated_at DATETIME,
                is_active INTEGER,
                metadata TEXT
            )";
        
        _db.ExecuteSQL(createTableSQL);

        var result = new BatchResult { BatchSize = batchSize };
        var sw = Stopwatch.StartNew();
        var insertedCount = 0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        while (insertedCount < TotalDocuments)
        {
            var currentBatchSize = Math.Min(batchSize, TotalDocuments - insertedCount);
            var batch = DataGenerator.GenerateBatch(currentBatchSize, insertedCount + 1);
            
            var batchStart = Stopwatch.GetTimestamp();
            
            // Insert batch
            var statements = batch.Select(doc => doc.ToInsertSQL(TableName)).ToList();
            _db.ExecuteBatchSQL(statements);
            _db.Flush();
            
            var batchElapsed = (Stopwatch.GetTimestamp() - batchStart) * 1000.0 / Stopwatch.Frequency;
            result.BatchLatencies.Add(batchElapsed);

            insertedCount += currentBatchSize;

            // Capture memory at milestones
            if (insertedCount >= 100_000 && result.MemoryAt100K == 0)
            {
                result.MemoryAt100K = GC.GetTotalMemory(false) - memoryBefore;
                Console.WriteLine($"[B2] Progress: 100K documents, Memory: {result.MemoryAt100K / 1024.0 / 1024.0:F2} MB");
            }
            
            if (insertedCount >= 500_000 && result.MemoryAt500K == 0)
            {
                result.MemoryAt500K = GC.GetTotalMemory(false) - memoryBefore;
                Console.WriteLine($"[B2] Progress: 500K documents, Memory: {result.MemoryAt500K / 1024.0 / 1024.0:F2} MB");
            }

            if (insertedCount % 100_000 == 0)
            {
                Console.WriteLine($"[B2] Progress: {insertedCount:N0}/{TotalDocuments:N0} documents inserted...");
            }

            await Task.Yield();
        }

        sw.Stop();
        result.MemoryAt1M = GC.GetTotalMemory(false) - memoryBefore;
        result.TotalTime = sw.Elapsed.TotalSeconds;
        result.Throughput = TotalDocuments / result.TotalTime;

        _results[batchSize] = result;

        Console.WriteLine($"[B2] Completed batch size {batchSize:N0}:");
        Console.WriteLine($"[B2]   Total Time: {result.TotalTime:F2}s");
        Console.WriteLine($"[B2]   Throughput: {result.Throughput:F0} docs/sec");
        Console.WriteLine($"[B2]   Avg Batch Latency: {result.BatchLatencies.Average():F2} ms");
        Console.WriteLine($"[B2]   Memory @ 100K: {result.MemoryAt100K / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"[B2]   Memory @ 500K: {result.MemoryAt500K / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"[B2]   Memory @ 1M:   {result.MemoryAt1M / 1024.0 / 1024.0:F2} MB");

        // Cleanup
        _db.Dispose();
        if (Directory.Exists(_dbPath))
        {
            Directory.Delete(_dbPath, recursive: true);
        }
    }

    private void PrintSummary()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("[B2] BENCHMARK SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine($"Total Documents Per Test: {TotalDocuments:N0}");
        Console.WriteLine();

        Console.WriteLine("Batch Size | Throughput (docs/sec) | Total Time | Avg Batch Latency | Memory @ 1M");
        Console.WriteLine("-----------|----------------------|------------|-------------------|------------");

        foreach (var batchSize in _batchSizes)
        {
            if (_results.TryGetValue(batchSize, out var result))
            {
                Console.WriteLine($"{batchSize,10:N0} | {result.Throughput,20:F0} | {result.TotalTime,10:F2}s | {result.BatchLatencies.Average(),17:F2} ms | {result.MemoryAt1M / 1024.0 / 1024.0,11:F2} MB");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Memory Growth Analysis:");
        foreach (var batchSize in _batchSizes)
        {
            if (_results.TryGetValue(batchSize, out var result))
            {
                Console.WriteLine($"  Batch {batchSize:N0}:");
                Console.WriteLine($"    @ 100K docs: {result.MemoryAt100K / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine($"    @ 500K docs: {result.MemoryAt500K / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine($"    @ 1M docs:   {result.MemoryAt1M / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine();
            }
        }

        Console.WriteLine("========================================");
    }

    public override void Teardown()
    {
        base.Teardown();
        Console.WriteLine($"[B2] Teardown: {ScenarioName}");
    }
}
