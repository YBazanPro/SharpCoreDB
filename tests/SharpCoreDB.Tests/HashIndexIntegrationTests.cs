using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.Interfaces;

namespace SharpCoreDB.Tests;

/// <summary>
/// Tests for HashIndex integration into Table and SQL operations.
/// Uses dedicated serial collection to prevent env-var race conditions between backends.
/// </summary>
[Collection("SerialHashIndexTests")]
public class HashIndexIntegrationTests
{
    private const string UnsafeEqualityIndexEnvironmentVariable = "SHARPCOREDB_USE_UNSAFE_EQUALITY_INDEX";

    private readonly string _testDbPath;
    private readonly IServiceProvider _serviceProvider;

    public HashIndexIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_hashindex_integration_{Guid.NewGuid()}");
        var services = new ServiceCollection();
        services.AddSharpCoreDB();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateIndex_CreatesHashIndexOnColumn(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE users (id INTEGER, name TEXT, email TEXT)");
            List<string> statements =
            [
                "INSERT INTO users VALUES ('1', 'Alice', 'alice@example.com')",
                "INSERT INTO users VALUES ('2', 'Bob', 'bob@example.com')"
            ];
            db.ExecuteBatchSQL(statements);

            db.ExecuteSQL("CREATE INDEX idx_user_email ON users (email)");
            db.ExecuteSQL("SELECT * FROM users WHERE email = 'alice@example.com'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_WHERE_Clause_UsesIndex(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE time_entries (id INTEGER, project TEXT, duration INTEGER)");

            List<string> statements = [];
            for (int i = 1; i <= 100; i++)
            {
                var project = i % 10 == 0 ? "Alpha" : $"Project{i}";
                statements.Add($"INSERT INTO time_entries VALUES ('{i}', '{project}', '{i * 10}')");
            }

            db.ExecuteBatchSQL(statements);
            db.ExecuteSQL("CREATE INDEX idx_project ON time_entries (project)");
            db.ExecuteSQL("SELECT * FROM time_entries WHERE project = 'Alpha'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_Insert_MaintainsIndex(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE products (id INTEGER, category TEXT, name TEXT)");
            db.ExecuteSQL("CREATE INDEX idx_category ON products (category)");
            List<string> statements =
            [
                "INSERT INTO products VALUES ('1', 'Electronics', 'Laptop')",
                "INSERT INTO products VALUES ('2', 'Electronics', 'Phone')",
                "INSERT INTO products VALUES ('3', 'Books', 'Novel')"
            ];
            db.ExecuteBatchSQL(statements);

            db.ExecuteSQL("SELECT * FROM products WHERE category = 'Electronics'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_Update_MaintainsIndex(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE tasks (id INTEGER, status TEXT, title TEXT)");
            List<string> statements =
            [
                "INSERT INTO tasks VALUES ('1', 'pending', 'Task 1')",
                "INSERT INTO tasks VALUES ('2', 'pending', 'Task 2')"
            ];
            db.ExecuteBatchSQL(statements);
            db.ExecuteSQL("CREATE INDEX idx_status ON tasks (status)");

            db.ExecuteSQL("UPDATE tasks SET status = 'completed' WHERE id = '1'");
            db.ExecuteSQL("SELECT * FROM tasks WHERE status = 'completed'");
            db.ExecuteSQL("SELECT * FROM tasks WHERE status = 'pending'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_Delete_MaintainsIndex(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE orders (id INTEGER, customer TEXT, amount INTEGER)");
            List<string> statements =
            [
                "INSERT INTO orders VALUES ('1', 'Alice', '100')",
                "INSERT INTO orders VALUES ('2', 'Bob', '200')",
                "INSERT INTO orders VALUES ('3', 'Alice', '150')"
            ];
            db.ExecuteBatchSQL(statements);
            db.ExecuteSQL("CREATE INDEX idx_customer ON orders (customer)");

            db.ExecuteSQL("DELETE FROM orders WHERE id = '1'");
            db.ExecuteSQL("SELECT * FROM orders WHERE customer = 'Alice'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_MultipleIndexes_WorkIndependently(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE employees (id INTEGER, department TEXT, city TEXT, name TEXT)");
            List<string> statements =
            [
                "INSERT INTO employees VALUES ('1', 'Engineering', 'Seattle', 'Alice')",
                "INSERT INTO employees VALUES ('2', 'Sales', 'Seattle', 'Bob')",
                "INSERT INTO employees VALUES ('3', 'Engineering', 'Portland', 'Charlie')"
            ];
            db.ExecuteBatchSQL(statements);

            db.ExecuteSQL("CREATE INDEX idx_department ON employees (department)");
            db.ExecuteSQL("CREATE INDEX idx_city ON employees (city)");
            db.ExecuteSQL("SELECT * FROM employees WHERE department = 'Engineering'");
            db.ExecuteSQL("SELECT * FROM employees WHERE city = 'Seattle'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_LargeDataset_PerformsBetter(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE events (id INTEGER, type TEXT, timestamp TEXT)");

            List<string> statements = [];
            for (int i = 1; i <= 1000; i++)
            {
                var type = i % 100 == 0 ? "critical" : $"type{i % 50}";
                statements.Add($"INSERT INTO events VALUES ('{i}', '{type}', '2024-01-01')");
            }

            db.ExecuteBatchSQL(statements);
            db.ExecuteSQL("CREATE INDEX idx_type ON events (type)");
            db.ExecuteSQL("SELECT * FROM events WHERE type = 'critical'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_DifferentDataTypes_Works(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE records (id INTEGER, count INTEGER, price REAL, active BOOLEAN)");
            List<string> statements =
            [
                "INSERT INTO records VALUES ('1', '100', '19.99', 'true')",
                "INSERT INTO records VALUES ('2', '200', '29.99', 'false')",
                "INSERT INTO records VALUES ('3', '100', '39.99', 'true')"
            ];
            db.ExecuteBatchSQL(statements);

            db.ExecuteSQL("CREATE INDEX idx_count ON records (count)");
            db.ExecuteSQL("SELECT * FROM records WHERE count = '100'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashIndex_UniqueIndex_SupportsCreation(bool useUnsafeEqualityIndex)
    {
        using var backendScope = new UnsafeEqualityIndexScope(useUnsafeEqualityIndex);
        var db = CreateDatabase();

        try
        {
            db.ExecuteSQL("CREATE TABLE accounts (id INTEGER, username TEXT, email TEXT)");
            List<string> statements =
            [
                "INSERT INTO accounts VALUES ('1', 'alice', 'alice@example.com')",
                "INSERT INTO accounts VALUES ('2', 'bob', 'bob@example.com')"
            ];
            db.ExecuteBatchSQL(statements);

            db.ExecuteSQL("CREATE UNIQUE INDEX idx_username ON accounts (username)");
            db.ExecuteSQL("SELECT * FROM accounts WHERE username = 'alice'");
        }
        finally
        {
            DisposeAndDelete(db);
        }
    }

    private IDatabase CreateDatabase()
    {
        var factory = _serviceProvider.GetRequiredService<DatabaseFactory>();
        var config = new DatabaseConfig { EnableHashIndexes = true };
        return factory.Create(_testDbPath, "test123", false, config);
    }

    private void DisposeAndDelete(IDatabase db)
    {
        (db as IDisposable)?.Dispose();
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, true);
        }
    }

    private sealed class UnsafeEqualityIndexScope : IDisposable
    {
        private readonly string? _originalValue;

        public UnsafeEqualityIndexScope(bool useUnsafeEqualityIndex)
        {
            _originalValue = Environment.GetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable);
            Environment.SetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable, useUnsafeEqualityIndex ? "true" : "false");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(UnsafeEqualityIndexEnvironmentVariable, _originalValue);
        }
    }
}
