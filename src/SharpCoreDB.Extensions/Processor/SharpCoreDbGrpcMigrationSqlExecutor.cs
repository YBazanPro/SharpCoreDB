using System.Data;
using SharpCoreDB.Client;

namespace SharpCoreDB.Extensions.Processor;

/// <summary>
/// Executes migration SQL against a remote SharpCoreDB server via gRPC.
/// </summary>
public sealed class SharpCoreDbGrpcMigrationSqlExecutor(SharpCoreDbGrpcMigrationOptions options) : ISharpCoreDbMigrationSqlExecutor
{
    private readonly SharpCoreDbGrpcMigrationOptions _options = options;

    /// <inheritdoc />
    public void ExecuteSql(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        ExecuteWithConnection(command =>
        {
            _ = command.ExecuteNonQueryAsync().GetAwaiter().GetResult();
            return 0;
        }, sql);
    }

    /// <inheritdoc />
    public object? ExecuteScalar(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return ExecuteWithConnection(command => command.ExecuteScalarAsync().GetAwaiter().GetResult(), sql);
    }

    /// <inheritdoc />
    public DataSet Read(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        return ExecuteWithConnection(command =>
        {
            var dataSet = new DataSet();
            var table = new DataTable("Result");

            using var reader = command.ExecuteReaderAsync().GetAwaiter().GetResult();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                table.Columns.Add(reader.GetName(i), typeof(object));
            }

            while (reader.ReadAsync().GetAwaiter().GetResult())
            {
                var row = table.NewRow();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }

                table.Rows.Add(row);
            }

            dataSet.Tables.Add(table);
            return dataSet;
        }, sql);
    }

    /// <inheritdoc />
    public IDbConnection? GetOperationConnection() => null;

    private T ExecuteWithConnection<T>(Func<SharpCoreDBCommand, T> execute, string sql)
    {
        ArgumentNullException.ThrowIfNull(execute);

        var connection = new SharpCoreDBConnection(_options.ConnectionString);

        try
        {
            connection.OpenAsync().GetAwaiter().GetResult();
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = _options.CommandTimeoutMs;

            return execute(command);
        }
        finally
        {
            connection.CloseAsync().GetAwaiter().GetResult();
            connection.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
