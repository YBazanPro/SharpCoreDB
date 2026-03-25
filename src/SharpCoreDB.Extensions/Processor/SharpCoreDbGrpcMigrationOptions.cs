namespace SharpCoreDB.Extensions.Processor;

/// <summary>
/// Options for running FluentMigrator migrations against a remote SharpCoreDB server over gRPC.
/// </summary>
public sealed record SharpCoreDbGrpcMigrationOptions(string ConnectionString)
{
    /// <summary>
    /// Gets the command timeout in milliseconds.
    /// </summary>
    public int CommandTimeoutMs { get; init; } = 30000;
}
