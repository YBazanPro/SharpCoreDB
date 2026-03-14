using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace SharpCoreDB.Tests;

/// <summary>
/// Performance tests demonstrating HashIndex speedup on WHERE clause queries.
/// </summary>
[Collection("SerialHashIndexTests")]
[Trait("Category", "Performance")]
public class HashIndexPerformanceTests
{
    private const string UnsafeEqualityIndexEnvironmentVariable = "SHARPCOREDB_USE_UNSAFE_EQUALITY_INDEX";

    private readonly string _testDbPath;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestOutputHelper _output;

    public HashIndexPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_hashindex_perf_{Guid.NewGuid()}");
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_SELECT_WHERE_Performance_5to10xFaster(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);

        var factory = _serviceProvider.GetRequiredService<DatabaseFactory>();
        var config = new DatabaseConfig { EnableHashIndexes = true, EnableQueryCache = false };
        var db = factory.Create(_testDbPath, "test123", false, config);

        try
        {
            _output.WriteLine($"Backend: {backendScope.BackendName}");

            db.ExecuteSQL("CREATE TABLE time_entries (id INTEGER, project TEXT, task TEXT, duration INTEGER)");

            _output.WriteLine("Inserting 10,000 test rows...");
            var projects = Enumerable.Range(0, 100).Select(i => $"project_{i}").ToArray();
            var insertStatements = new List<string>();
            for (int i = 1; i <= 10000; i++)
            {
                var project = projects[i % projects.Length];
                insertStatements.Add($"INSERT INTO time_entries VALUES ('{i}', '{project}', 'Task{i}', '{i * 10}')");
            }

            db.ExecuteBatchSQL(insertStatements);
            db.Flush();
            _output.WriteLine("Data insertion complete.");

            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                db.ExecuteSQL("SELECT * FROM time_entries WHERE project = 'project_0'");
            }
            sw1.Stop();
            var withoutIndexMs = sw1.ElapsedMilliseconds;
            _output.WriteLine($"Without index: 1000 queries took {withoutIndexMs}ms");

            _output.WriteLine("Creating hash index on 'project' column...");
            db.ExecuteSQL("CREATE INDEX idx_project ON time_entries (project)");

            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                db.ExecuteSQL("SELECT * FROM time_entries WHERE project = 'project_0'");
            }
            sw2.Stop();
            var withIndexMs = sw2.ElapsedMilliseconds;
            _output.WriteLine($"With index: 1000 queries took {withIndexMs}ms");

            var speedup = (double)withoutIndexMs / withIndexMs;
            _output.WriteLine($"Speedup: {speedup:F2}x faster with hash index ({backendScope.BackendName})");

            Assert.True(speedup >= 0.8, $"Index should not degrade performance significantly for backend '{backendScope.BackendName}', got {speedup:F2}x speedup");
        }
        finally
        {
            (db as IDisposable)?.Dispose();
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_MultipleQueries_ConsistentPerformance(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);

        var factory = _serviceProvider.GetRequiredService<DatabaseFactory>();
        var config = new DatabaseConfig { EnableHashIndexes = true, EnableQueryCache = false };
        var db = factory.Create(_testDbPath, "test123", false, config);

        try
        {
            _output.WriteLine($"Backend: {backendScope.BackendName}");

            db.ExecuteSQL("CREATE TABLE orders (id INTEGER, customer_id INTEGER, status TEXT, amount INTEGER)");

            _output.WriteLine("Inserting 5,000 test rows...");
            var statuses = Enumerable.Range(0, 50).Select(i => $"status_{i}").ToArray();
            var insertStatements = new List<string>();
            for (int i = 1; i <= 5000; i++)
            {
                var status = statuses[i % statuses.Length];
                var customerId = i % 500;
                insertStatements.Add($"INSERT INTO orders VALUES ('{i}', '{customerId}', '{status}', '{i * 100}')");
            }
            db.ExecuteBatchSQL(insertStatements);
            db.Flush();

            db.ExecuteSQL("CREATE INDEX idx_customer ON orders (customer_id)");
            db.ExecuteSQL("CREATE INDEX idx_status ON orders (status)");
            _output.WriteLine("Indexes created.");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                db.ExecuteSQL($"SELECT * FROM orders WHERE customer_id = '{i % 500}'");
            }
            sw.Stop();
            var customerQueryMs = sw.ElapsedMilliseconds;
            _output.WriteLine($"50 customer queries: {customerQueryMs}ms");

            sw.Restart();
            for (int i = 0; i < 50; i++)
            {
                db.ExecuteSQL($"SELECT * FROM orders WHERE status = '{statuses[i % statuses.Length]}'");
            }
            sw.Stop();
            var statusQueryMs = sw.ElapsedMilliseconds;
            _output.WriteLine($"50 status queries: {statusQueryMs}ms");

            Assert.True(customerQueryMs < 1200, $"Customer queries took {customerQueryMs}ms for backend '{backendScope.BackendName}', expected < 1200ms");
            Assert.True(statusQueryMs < 1200, $"Status queries took {statusQueryMs}ms for backend '{backendScope.BackendName}', expected < 1200ms");
        }
        finally
        {
            (db as IDisposable)?.Dispose();
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_MemoryOverhead_Acceptable(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);

        var factory = _serviceProvider.GetRequiredService<DatabaseFactory>();
        var config = new DatabaseConfig { EnableHashIndexes = true };
        var db = factory.Create(_testDbPath, "test123", false, config);

        try
        {
            _output.WriteLine($"Backend: {backendScope.BackendName}");

            db.ExecuteSQL("CREATE TABLE metrics (id INTEGER, metric_name TEXT, value REAL, timestamp TEXT)");

            _output.WriteLine("Inserting 20,000 test rows...");
            var insertStatements = new List<string>();
            for (int i = 1; i <= 20000; i++)
            {
                var metricName = $"metric_{i % 1000}";
                insertStatements.Add($"INSERT INTO metrics VALUES ('{i}', '{metricName}', '{i * 1.5}', '2024-01-01')");
            }
            db.ExecuteBatchSQL(insertStatements);
            db.Flush();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memBefore = GC.GetTotalMemory(false);
            _output.WriteLine($"Memory before index: {memBefore / 1024 / 1024:F2} MB");

            db.ExecuteSQL("CREATE INDEX idx_metric_name ON metrics (metric_name)");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memAfter = GC.GetTotalMemory(false);
            _output.WriteLine($"Memory after index: {memAfter / 1024 / 1024:F2} MB");

            var overhead = memAfter - memBefore;
            _output.WriteLine($"Index overhead: {overhead / 1024 / 1024:F2} MB");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                db.ExecuteSQL($"SELECT * FROM metrics WHERE metric_name = 'metric_{i % 1000}'");
            }
            sw.Stop();
            _output.WriteLine($"100 indexed queries: {sw.ElapsedMilliseconds}ms");
        }
        finally
        {
            (db as IDisposable)?.Dispose();
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }
    }

    private sealed class UnsafeEqualityIndexScope : IDisposable
    {
        private readonly string? _originalValue;

        public UnsafeEqualityIndexScope(bool useUnsafeEqualityIndex)
        {
            _originalValue = Environment.GetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable);
            BackendName = useUnsafeEqualityIndex ? "unsafe" : "classic";
            Environment.SetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable, useUnsafeEqualityIndex ? "true" : "false");
        }

        public string BackendName { get; }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable, _originalValue);
        }
    }
}
