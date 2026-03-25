using FluentMigrator.Runner.VersionTableInfo;

namespace SharpCoreDB.Extensions.Processor;

/// <summary>
/// Version table metadata for SharpCoreDB FluentMigrator integration.
/// </summary>
public sealed class SharpCoreDbVersionTableMetaData : IVersionTableMetaData
{
    /// <inheritdoc />
    public object ApplicationContext { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool OwnsSchema => false;

    /// <inheritdoc />
    public string SchemaName => string.Empty;

    /// <inheritdoc />
    public string TableName => "__SharpMigrations";

    /// <inheritdoc />
    public string ColumnName => "Version";

    /// <inheritdoc />
    public string DescriptionColumnName => "Description";

    /// <inheritdoc />
    public string UniqueIndexName => "UX___SharpMigrations_Version";

    /// <inheritdoc />
    public string AppliedOnColumnName => "AppliedOn";

    /// <inheritdoc />
    public bool CreateWithPrimaryKey => true;
}
