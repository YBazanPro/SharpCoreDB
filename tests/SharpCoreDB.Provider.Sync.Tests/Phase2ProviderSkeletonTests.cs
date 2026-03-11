#nullable enable

using System.Data;
using Dotmim.Sync;
using FluentAssertions;
using SharpCoreDB.Provider.Sync.Adapters;
using SharpCoreDB.Provider.Sync.Metadata;
using Xunit;

namespace SharpCoreDB.Provider.Sync.Tests;

/// <summary>
/// Tests for Phase 2 implementations: SharpCoreDBMetadata, SharpCoreDBObjectNames, SharpCoreDBSchemaReader.
/// </summary>
public sealed class Phase2ProviderSkeletonTests
{
    // ── SharpCoreDBMetadata ─────────────────────────────────────────────

    [Theory]
    [InlineData("INTEGER", true)]
    [InlineData("TEXT", true)]
    [InlineData("REAL", true)]
    [InlineData("BLOB", true)]
    [InlineData("BIGINT", true)]
    [InlineData("BOOLEAN", true)]
    [InlineData("DECIMAL", true)]
    [InlineData("GUID", true)]
    [InlineData("DATETIME", true)]
    [InlineData("FOOBAR", false)]
    public void Metadata_IsValid_RecognisesTypes(string typeName, bool expected)
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("test") { OriginalTypeName = typeName };

        // Act & Assert
        metadata.IsValid(col).Should().Be(expected);
    }

    [Theory]
    [InlineData("INTEGER", DbType.Int32)]
    [InlineData("BIGINT", DbType.Int64)]
    [InlineData("TEXT", DbType.String)]
    [InlineData("REAL", DbType.Double)]
    [InlineData("BLOB", DbType.Binary)]
    [InlineData("BOOLEAN", DbType.Boolean)]
    [InlineData("DECIMAL", DbType.Decimal)]
    [InlineData("GUID", DbType.Guid)]
    [InlineData("DATETIME", DbType.DateTime)]
    public void Metadata_GetDbType_MapsCorrectly(string typeName, DbType expected)
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("test") { OriginalTypeName = typeName };

        // Act & Assert
        metadata.GetDbType(col).Should().Be(expected);
    }

    [Theory]
    [InlineData("INTEGER", typeof(int))]
    [InlineData("BIGINT", typeof(long))]
    [InlineData("TEXT", typeof(string))]
    [InlineData("REAL", typeof(double))]
    [InlineData("BLOB", typeof(byte[]))]
    [InlineData("BOOLEAN", typeof(bool))]
    [InlineData("DECIMAL", typeof(decimal))]
    [InlineData("GUID", typeof(Guid))]
    [InlineData("DATETIME", typeof(DateTime))]
    public void Metadata_GetType_MapsCorrectly(string typeName, Type expected)
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("test") { OriginalTypeName = typeName };

        // Act & Assert
        metadata.GetType(col).Should().Be(expected);
    }

    [Theory]
    [InlineData("INTEGER", true)]
    [InlineData("BIGINT", true)]
    [InlineData("REAL", true)]
    [InlineData("DECIMAL", true)]
    [InlineData("TEXT", false)]
    [InlineData("BLOB", false)]
    public void Metadata_IsNumericType_ReturnsCorrectly(string typeName, bool expected)
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("test") { OriginalTypeName = typeName };

        // Act & Assert
        metadata.IsNumericType(col).Should().Be(expected);
    }

    [Theory]
    [InlineData("REAL", true)]
    [InlineData("DECIMAL", true)]
    [InlineData("INTEGER", false)]
    [InlineData("TEXT", false)]
    public void Metadata_IsSupportingScale_ReturnsCorrectly(string typeName, bool expected)
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("test") { OriginalTypeName = typeName };

        // Act & Assert
        metadata.IsSupportingScale(col).Should().Be(expected);
    }

    [Fact]
    public void Metadata_GetPrecisionAndScale_Decimal_ReturnsDefault()
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("price") { OriginalTypeName = "DECIMAL" };

        // Act
        var (precision, scale) = metadata.GetPrecisionAndScale(col);

        // Assert
        precision.Should().Be(18);
        scale.Should().Be(6);
    }

    [Fact]
    public void Metadata_IsReadonly_ReturnsTrueForComputed()
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();
        var col = new SyncColumn("computed") { OriginalTypeName = "INTEGER", IsCompute = true };

        // Act & Assert
        metadata.IsReadonly(col).Should().BeTrue();
    }

    [Fact]
    public void Metadata_NormalizesAliases()
    {
        // Arrange
        var metadata = new SharpCoreDBMetadata();

        // Act & Assert — "int" and "System.Int32" should both map to INTEGER → Int32
        metadata.GetDbType(new SyncColumn("a") { OriginalTypeName = "int" }).Should().Be(DbType.Int32);
        metadata.GetDbType(new SyncColumn("b") { OriginalTypeName = "System.Int32" }).Should().Be(DbType.Int32);
        metadata.GetDbType(new SyncColumn("c") { OriginalTypeName = "nvarchar" }).Should().Be(DbType.String);
    }

    // ── SharpCoreDBSyncProvider.GetMetadata() ───────────────────────────

    [Fact]
    public void SyncProvider_GetMetadata_ReturnsSharpCoreDBMetadata()
    {
        // Arrange
        var provider = new SharpCoreDBSyncProvider("Path=test.scdb;Password=x", new SyncProviderOptions());

        // Act
        var metadata = provider.GetMetadata();

        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().BeOfType<SharpCoreDBMetadata>();
    }

    // ── SharpCoreDBObjectNames ──────────────────────────────────────────

    [Fact]
    public void ObjectNames_SelectChangesCommand_ContainsTrackingJoin()
    {
        // Arrange
        var table = CreateTestSyncTable();
        var names = new SharpCoreDBObjectNames(table);

        // Act
        var sql = names.SelectChangesCommand;

        // Assert
        sql.Should().Contain("[users_tracking]");
        sql.Should().Contain("@sync_min_timestamp");
        sql.Should().Contain("[id]");
        sql.Should().Contain("[name]");
    }

    [Fact]
    public void ObjectNames_InsertCommand_ContainsAllColumns()
    {
        // Arrange
        var table = CreateTestSyncTable();
        var names = new SharpCoreDBObjectNames(table);

        // Act
        var sql = names.InsertCommand;

        // Assert
        sql.Should().Contain("INSERT OR REPLACE INTO [users]");
        sql.Should().Contain("@id");
        sql.Should().Contain("@name");
    }

    [Fact]
    public void ObjectNames_UpdateCommand_ExcludesPkFromSet()
    {
        // Arrange
        var table = CreateTestSyncTable();
        var names = new SharpCoreDBObjectNames(table);

        // Act
        var sql = names.UpdateCommand;

        // Assert
        sql.Should().Contain("SET [name] = @name");
        sql.Should().Contain("WHERE [id] = @id");
        // SET clause should not include the PK column itself
        sql.Should().NotContain("SET [id]");
    }

    [Fact]
    public void ObjectNames_DeleteCommand_UsesPrimaryKey()
    {
        // Arrange
        var table = CreateTestSyncTable();
        var names = new SharpCoreDBObjectNames(table);

        // Act
        var sql = names.DeleteCommand;

        // Assert
        sql.Should().Contain("DELETE FROM [users]");
        sql.Should().Contain("WHERE [id] = @id");
    }

    [Fact]
    public void ObjectNames_ResetCommand_DeletesTrackingAndData()
    {
        // Arrange
        var table = CreateTestSyncTable();
        var names = new SharpCoreDBObjectNames(table);

        // Act
        var sql = names.ResetCommand;

        // Assert
        sql.Should().Contain("DELETE FROM [users_tracking]");
        sql.Should().Contain("DELETE FROM [users]");
    }

    [Fact]
    public void ObjectNames_NoPrimaryKey_ThrowsInvalidOperation()
    {
        // Arrange — table without PK
        var table = new SyncTable("broken");
        table.Columns.Add(new SyncColumn("col1") { OriginalTypeName = "TEXT" });
        var names = new SharpCoreDBObjectNames(table);

        // Act & Assert
        var act = () => names.SelectRowCommand;
        act.Should().Throw<InvalidOperationException>().WithMessage("*primary key*");
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static SyncTable CreateTestSyncTable()
    {
        var table = new SyncTable("users");
        table.Columns.Add(new SyncColumn("id") { OriginalTypeName = "INTEGER", DbType = (int)DbType.Int32 });
        table.Columns.Add(new SyncColumn("name") { OriginalTypeName = "TEXT", DbType = (int)DbType.String });
        table.PrimaryKeys.Add("id");
        return table;
    }
}
