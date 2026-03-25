using System.Collections;
using System.Data;
using System.Data.Common;
using FluentMigrator;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharpCoreDB.Extensions.Extensions;
using SharpCoreDB.Extensions.Processor;
using SharpCoreDB.Interfaces;

namespace SharpCoreDB.Tests;

public sealed class SharpCoreDbMigrationExecutorTests
{
    [Fact]
    public void ExecuteSql_WhenCustomExecutorRegistered_UsesCustomExecutorBeforeFallbacks()
    {
        // Arrange
        var customExecutor = new Mock<ISharpCoreDbMigrationSqlExecutor>(MockBehavior.Strict);
        customExecutor.Setup(x => x.ExecuteSql("CREATE TABLE audit (id INT)"));

        var fallbackConnection = new FakeDbConnection();

        var services = new ServiceCollection();
        services.AddSingleton(customExecutor.Object);
        services.AddSingleton<DbConnection>(fallbackConnection);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        // Act
        executor.ExecuteSql("CREATE TABLE audit (id INT)");

        // Assert
        customExecutor.Verify(x => x.ExecuteSql("CREATE TABLE audit (id INT)"), Times.Once);
        Assert.Empty(fallbackConnection.ExecutedNonQuerySql);
    }

    [Fact]
    public void AddSharpCoreDBFluentMigratorGrpc_WhenCalled_RegistersGrpcSqlExecutor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSharpCoreDBFluentMigratorGrpc("Server=localhost;Port=5001;Database=master;SSL=false;Username=admin;Password=test");

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<ISharpCoreDbMigrationSqlExecutor>();

        // Assert
        Assert.IsType<SharpCoreDbGrpcMigrationSqlExecutor>(executor);
    }

    [Fact]
    public void ExecuteSql_WhenDatabaseRegistered_UsesDatabaseExecution()
    {
        // Arrange
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock
            .Setup(x => x.ExecuteSQL("CREATE TABLE users (id INT)"));

        var services = new ServiceCollection();
        services.AddSingleton(databaseMock.Object);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        // Act
        executor.ExecuteSql("CREATE TABLE users (id INT)");

        // Assert
        databaseMock.Verify(x => x.ExecuteSQL("CREATE TABLE users (id INT)"), Times.Once);
    }

    [Fact]
    public void ExecuteSql_WhenConnectionRegistered_UsesConnectionCommand()
    {
        // Arrange
        var connection = new FakeDbConnection();
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(connection);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        // Act
        executor.ExecuteSql("CREATE TABLE products (id INT)");

        // Assert
        Assert.Contains("CREATE TABLE products (id INT)", connection.ExecutedNonQuerySql);
    }

    [Fact]
    public void EnsureVersionTable_WhenCalled_CreatesSharpMigrationsTable()
    {
        // Arrange
        var connection = new FakeDbConnection();
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(connection);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        // Act
        SharpCoreDbMigrationExecutor.EnsureVersionTable(executor);

        // Assert
        Assert.Contains(connection.ExecutedNonQuerySql, sql => sql.Contains("__SharpMigrations", StringComparison.Ordinal));
    }

    [Fact]
    public void Read_WhenConnectionReturnsReader_MapsRowsIntoDataSet()
    {
        // Arrange
        var connection = new FakeDbConnection
        {
            ReaderFactory = _ =>
            {
                var table = new DataTable();
                table.Columns.Add("id", typeof(int));
                table.Columns.Add("name", typeof(string));
                table.Rows.Add(1, "Alice");
                return table.CreateDataReader();
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(connection);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        // Act
        var dataSet = executor.Read("SELECT id, name FROM users");

        // Assert
        Assert.Single(dataSet.Tables);
        Assert.Single(dataSet.Tables[0].Rows);
        Assert.Equal("Alice", dataSet.Tables[0].Rows[0]["name"]);
    }

    [Fact]
    public void ProcessorExecute_WhenConnectionRegistered_ExecutesSqlViaExecutor()
    {
        // Arrange
        var connection = new FakeDbConnection();
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(connection);

        var executor = new SharpCoreDbMigrationExecutor(services.BuildServiceProvider());

        var processorOptions = new Mock<IMigrationProcessorOptions>(MockBehavior.Strict);
        processorOptions.SetupGet(x => x.PreviewOnly).Returns(false);
        processorOptions.SetupGet(x => x.ProviderSwitches).Returns(string.Empty);
        processorOptions.SetupGet(x => x.Timeout).Returns((int?)null);

        var processor = new SharpCoreDbProcessor("sharpcoredb://test", processorOptions.Object, executor);

        // Act
        processor.Execute("DELETE FROM users");

        // Assert
        Assert.Contains("DELETE FROM users", connection.ExecutedNonQuerySql);
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public List<string> ExecutedNonQuerySql { get; } = [];

        public Func<string, object?>? ScalarFactory { get; set; }

        public Func<string, DbDataReader>? ReaderFactory { get; set; }

        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "FakeSharpCoreDB";

        public override string DataSource => "Fake";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new FakeDbCommand(this);
        }
    }

    private sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
    {
        private readonly FakeDbConnection _connection = connection;
        private string _commandText = string.Empty;

        public override string CommandText
        {
            get => _commandText;
            set => _commandText = value;
        }

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; } = CommandType.Text;

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection DbConnection { get; set; } = connection;

        protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            _connection.ExecutedNonQuerySql.Add(CommandText);
            return 1;
        }

        public override object? ExecuteScalar()
        {
            return _connection.ScalarFactory?.Invoke(CommandText);
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter()
        {
            return new FakeDbParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return _connection.ReaderFactory?.Invoke(CommandText) ?? new DataTable().CreateDataReader();
        }
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;

        public override object SyncRoot => _items;

        public override int Add(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                _ = Add(value!);
            }
        }

        public override void Clear() => _items.Clear();

        public override bool Contains(object value) => _items.Contains((DbParameter)value);

        public override bool Contains(string value) => _items.Any(x => x.ParameterName == value);

        public override void CopyTo(Array array, int index)
        {
            _items.ToArray().CopyTo(array, index);
        }

        public override IEnumerator GetEnumerator() => _items.GetEnumerator();

        protected override DbParameter GetParameter(int index) => _items[index];

        protected override DbParameter GetParameter(string parameterName) => _items.First(x => x.ParameterName == parameterName);

        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName) => _items.FindIndex(x => x.ParameterName == parameterName);

        public override void Insert(int index, object value)
        {
            _items.Insert(index, (DbParameter)value);
        }

        public override void Remove(object value)
        {
            _items.Remove((DbParameter)value);
        }

        public override void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            _items[index] = value;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
            {
                _items.Add(value);
                return;
            }

            _items[index] = value;
        }
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

        public override bool IsNullable { get; set; }

        public override string ParameterName { get; set; } = string.Empty;

        public override string SourceColumn { get; set; } = string.Empty;

        public override object? Value { get; set; }

        public override bool SourceColumnNullMapping { get; set; }

        public override int Size { get; set; }

        public override void ResetDbType()
        {
        }
    }
}
