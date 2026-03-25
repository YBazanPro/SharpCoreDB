using System.Data;

namespace SharpCoreDB.Extensions.Processor;

/// <summary>
/// Contract for executing FluentMigrator SQL statements against a SharpCoreDB target.
/// </summary>
public interface ISharpCoreDbMigrationSqlExecutor
{
    /// <summary>
    /// Executes a SQL statement that does not return a result set.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    void ExecuteSql(string sql);

    /// <summary>
    /// Executes a SQL statement and returns the first scalar value.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <returns>The first scalar value, or <see langword="null"/>.</returns>
    object? ExecuteScalar(string sql);

    /// <summary>
    /// Executes a SQL statement and returns tabular results.
    /// </summary>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <returns>A populated <see cref="DataSet"/> instance.</returns>
    DataSet Read(string sql);

    /// <summary>
    /// Gets an optional open operation connection for DB-operation expressions.
    /// </summary>
    /// <returns>An open <see cref="IDbConnection"/>, or <see langword="null"/> if unavailable.</returns>
    IDbConnection? GetOperationConnection();
}
