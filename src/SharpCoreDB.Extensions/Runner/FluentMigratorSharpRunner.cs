using FluentMigrator.Runner;

namespace SharpCoreDB.Extensions.Runner;

/// <summary>
/// Abstraction to execute FluentMigrator migrations with SharpCoreDB.
/// </summary>
public interface ISharpCoreDbMigrationRunner
{
    /// <summary>
    /// Runs all pending migrations.
    /// </summary>
    void MigrateUp();

    /// <summary>
    /// Runs migrations up to a specific version.
    /// </summary>
    /// <param name="version">The target migration version.</param>
    void MigrateUpTo(long version);

    /// <summary>
    /// Rolls back migrations by step count.
    /// </summary>
    /// <param name="steps">The number of versions to rollback.</param>
    void Rollback(int steps);
}

/// <summary>
/// Default runner wrapper for FluentMigrator.
/// </summary>
public sealed class FluentMigratorSharpRunner(IMigrationRunner runner) : ISharpCoreDbMigrationRunner
{
    private readonly IMigrationRunner _runner = runner;

    /// <inheritdoc />
    public void MigrateUp() => _runner.MigrateUp();

    /// <inheritdoc />
    public void MigrateUpTo(long version) => _runner.MigrateUp(version);

    /// <inheritdoc />
    public void Rollback(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);
        _runner.Rollback(steps);
    }
}
