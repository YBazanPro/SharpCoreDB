using System.Text.Json;
using HybridStorageMode = SharpCoreDB.Storage.Hybrid.StorageMode;
using SharpCoreDB.Core.Serialization;
using SharpCoreDB.Storage;
using SharpCoreDB.Storage.Hybrid;
using Xunit;

namespace SharpCoreDB.Tests;

/// <summary>
/// Focused tests for <see cref="StorageMigrator"/> helper behavior.
/// Validates the remaining migration backlog paths without broad integration cost.
/// </summary>
public sealed class StorageMigratorTests : IDisposable
{
    private readonly string _databasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageMigratorTests"/> class.
    /// </summary>
    public StorageMigratorTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"storage_migrator_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_databasePath);
    }

    [Fact]
    public async Task MigrateToPageBased_WithLengthPrefixedColumnarData_ShouldCreatePageFileAndMetadata()
    {
        // Arrange
        var tableName = "users";
        var columnarPath = Path.Combine(_databasePath, $"{tableName}.dat");
        await WriteColumnarRowsAsync(columnarPath,
        [
            new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice" },
            new Dictionary<string, object> { ["id"] = 2, ["name"] = "Bob" }
        ]);

        var migrator = new StorageMigrator(_databasePath);

        // Act
        var success = await migrator.MigrateToPageBased(tableName, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);

        var pagePath = GetPagePath(tableName);
        Assert.True(File.Exists(pagePath));
        Assert.True(File.Exists(Path.Combine(_databasePath, $"_archived_{tableName}.dat")));

        var metadata = await ReadMetadataAsync();
        Assert.True(metadata.TryGetValue(tableName, out var tableMetadata));
        Assert.Equal(HybridStorageMode.PageBased, tableMetadata!.StorageMode);
        Assert.Equal(pagePath, tableMetadata.DataFilePath);
    }

    [Fact]
    public async Task MigrateToColumnar_WithPageBasedData_ShouldCreateColumnarFileAndMetadata()
    {
        // Arrange
        var tableName = "orders";
        var pagePath = GetPagePath(tableName);
        CreatePageBasedRows(tableName,
        [
            new Dictionary<string, object> { ["id"] = 10, ["amount"] = 42.5d },
            new Dictionary<string, object> { ["id"] = 11, ["amount"] = 64.0d }
        ]);
        Assert.True(File.Exists(pagePath));

        var migrator = new StorageMigrator(_databasePath);

        // Act
        var success = await migrator.MigrateToColumnar(tableName, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);

        var columnarPath = Path.Combine(_databasePath, $"{tableName}.dat");
        Assert.True(File.Exists(columnarPath));
        Assert.True(File.Exists(Path.Combine(_databasePath, $"_archived_{Path.GetFileName(pagePath)}")));

        var rows = await ReadColumnarRowsAsync(columnarPath, TestContext.Current.CancellationToken);
        Assert.Equal(2, rows.Count);

        var metadata = await ReadMetadataAsync();
        Assert.True(metadata.TryGetValue(tableName, out var tableMetadata));
        Assert.Equal(HybridStorageMode.Columnar, tableMetadata!.StorageMode);
        Assert.Equal(columnarPath, tableMetadata.DataFilePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_databasePath))
            {
                Directory.Delete(_databasePath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in temp paths.
        }
    }

    private static async Task WriteColumnarRowsAsync(string path, IReadOnlyList<Dictionary<string, object>> rows)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        foreach (var row in rows)
        {
            var payload = BinaryRowSerializer.Serialize(row);
            var lengthBytes = BitConverter.GetBytes(payload.Length);
            await stream.WriteAsync(lengthBytes);
            await stream.WriteAsync(payload);
        }
    }

    private void CreatePageBasedRows(string tableName, IReadOnlyList<Dictionary<string, object>> rows)
    {
        using var manager = new PageManager(_databasePath, GetTableId(tableName));
        foreach (var row in rows)
        {
            var payload = BinaryRowSerializer.Serialize(row);
            var pageId = manager.FindPageWithSpace(GetTableId(tableName), payload.Length);
            manager.InsertRecord(pageId, payload);
        }
        manager.FlushDirtyPages();
    }

    private async Task<Dictionary<string, TableMetadataExtended>> ReadMetadataAsync()
    {
        var metadataPath = Path.Combine(_databasePath, "meta.dat");
        await using var stream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, TableMetadataExtended>>(stream, cancellationToken: TestContext.Current.CancellationToken)
            ?? [];
    }

    private static async Task<List<Dictionary<string, object>>> ReadColumnarRowsAsync(string path, CancellationToken cancellationToken)
    {
        var result = new List<Dictionary<string, object>>();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var lengthBuffer = new byte[sizeof(int)];

        while (stream.Position < stream.Length)
        {
            await stream.ReadExactlyAsync(lengthBuffer, cancellationToken);
            var length = BitConverter.ToInt32(lengthBuffer);
            var payload = new byte[length];
            await stream.ReadExactlyAsync(payload, cancellationToken);
            result.Add(BinaryRowSerializer.Deserialize(payload));
        }

        return result;
    }

    private string GetPagePath(string tableName) =>
        Path.Combine(_databasePath, $"table_{GetTableId(tableName)}.pages");

    private static uint GetTableId(string tableName) => (uint)tableName.GetHashCode();
}
