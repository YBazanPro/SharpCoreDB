#nullable enable

using FluentAssertions;
using Xunit;
using SharpCoreDB.Graph.Advanced.GraphRAG;
using SharpCoreDB.Graph.Advanced.SqlIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreDB.Graph.Advanced.Tests;

/// <summary>
/// Integration tests for GraphRAG functionality.
/// Tests end-to-end GraphRAG operations with real database.
/// </summary>
public class GraphRagTests : IDisposable
{
    private readonly Database _database;
    private readonly GraphRagEngine _engine;
    private readonly string _graphTable = "test_graph";
    private readonly string _embeddingTable = "test_embeddings";
    private readonly int _embeddingDimensions = 128;
    private bool _disposed;

    public GraphRagTests()
    {
        // Create test database
        var services = new ServiceCollection()
            .AddSharpCoreDB()
            .BuildServiceProvider();

        _database = new Database(
            services,
            Path.Combine(Path.GetTempPath(), $"graphrag_test_{Guid.NewGuid()}"),
            "test_password",
            isReadOnly: false);

        // Initialize GraphRAG engine
        _engine = new GraphRagEngine(_database, _graphTable, _embeddingTable, _embeddingDimensions);

        // Setup test data
        SetupTestDataAsync().GetAwaiter().GetResult();
    }

    private async Task SetupTestDataAsync()
    {
        // Create graph table
        _database.ExecuteSQL($@"
            CREATE TABLE {_graphTable} (
                source INTEGER,
                target INTEGER,
                weight REAL DEFAULT 1.0
            )
        ");

        // Create a small social network
        (int source, int target)[] edges =
        [
            (1, 2), (2, 1),
            (2, 3), (3, 2),
            (3, 4), (4, 3),
            (4, 5), (5, 4),
            (1, 3), (3, 1),
            (2, 4), (4, 2)
        ];

        _database.ExecuteBatchSQL(edges.Select(edge => $"INSERT INTO {_graphTable} (source, target) VALUES ({edge.source}, {edge.target})"));
        _database.Flush();

        // Initialize GraphRAG system
        await _engine.InitializeAsync();

        // Generate and index embeddings
        ulong[] nodeIds = [1, 2, 3, 4, 5];
        var embeddings = VectorSearchIntegration.GenerateMockEmbeddings(nodeIds, _embeddingDimensions);
        await _engine.IndexEmbeddingsAsync(embeddings);
    }

    [Fact]
    public async Task GraphRagEngine_Initialization_CreatesTables()
    {
        // Verify tables exist
        var graphExists = GraphLoader.ValidateGraphTable(_database, _graphTable);
        var embeddingExists = VectorSearchIntegration.ValidateVectorTable(_database, _embeddingTable);

        graphExists.Should().BeTrue();
        embeddingExists.Should().BeTrue();
    }

    [Fact]
    public async Task GraphRagEngine_SearchAsync_ReturnsRankedResults()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;

        // Act
        var results = await _engine.SearchAsync(queryEmbedding, topK: 3);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.CombinedScore.Should().BeGreaterThanOrEqualTo(0));
        results.Should().AllSatisfy(r => r.CombinedScore.Should().BeLessThanOrEqualTo(1));

        // Results should be sorted by combined score (descending)
        for (int i = 1; i < results.Count; i++)
        {
            results[i - 1].CombinedScore.Should().BeGreaterThanOrEqualTo(results[i].CombinedScore);
        }
    }

    [Fact]
    public async Task GraphRagEngine_SearchAsync_WithCommunities_IncludesCommunityInfo()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;

        // Act
        var results = await _engine.SearchAsync(queryEmbedding, topK: 3, includeCommunities: true);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.CombinedScore.Should().BeGreaterThanOrEqualTo(0));
        results.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Context));
        results.Should().AllSatisfy(r => r.CommunityScore.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    public async Task GraphRagEngine_SearchAsync_WithMultiHop_ConsidersGraphDistance()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;

        // Act
        var results = await _engine.SearchAsync(queryEmbedding, topK: 3, maxHops: 2);

        // Assert
        results.Should().HaveCount(3);
        // Multi-hop should consider graph structure
        results.Should().AllSatisfy(r => r.CombinedScore.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    public async Task GraphRagEngine_GetNodeContextAsync_ReturnsComprehensiveContext()
    {
        // Act
        var context = await _engine.GetNodeContextAsync(nodeId: 1, maxDistance: 2);

        // Assert
        context.NodeId.Should().Be(1);
        context.CommunityMembers.Should().NotBeEmpty();
        context.GraphNeighbors.Should().NotBeEmpty();
        context.SemanticNeighbors.Should().NotBeEmpty();
        context.ContextDescription.Should().NotBeNullOrEmpty();
        context.ContextDescription.Should().Contain("Node 1");
    }

    [Fact]
    public async Task VectorSearchIntegration_SemanticSimilaritySearch_FindsSimilarNodes()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;

        // Act
        var results = await VectorSearchIntegration.SemanticSimilaritySearchAsync(
            _database, _embeddingTable, queryEmbedding, topK: 3);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.similarityScore.Should().BeGreaterThanOrEqualTo(0));
        results.Should().AllSatisfy(r => r.similarityScore.Should().BeLessThanOrEqualTo(1));

        // First result should be the query node itself (perfect similarity)
        results[0].nodeId.Should().Be(1);
        results[0].similarityScore.Should().BeGreaterThan(0.99);
    }

    [Fact]
    public async Task EnhancedRanking_RankResults_CombinesMultipleFactors()
    {
        // Arrange
        var semanticResults = new List<(ulong nodeId, double semanticScore)>
        {
            (1, 0.9), (2, 0.8), (3, 0.7)
        };

        var graphData = await GraphLoader.LoadFromTableAsync(_database, _graphTable);
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _graphTable);

        // Act
        var rankedResults = EnhancedRanking.RankResults(semanticResults, graphData, communities);

        // Assert
        rankedResults.Should().HaveCount(3);
        rankedResults.Should().AllSatisfy(r => r.CombinedScore.Should().BeGreaterThanOrEqualTo(0));
        rankedResults.Should().AllSatisfy(r => r.SemanticScore.Should().BeGreaterThan(0));
        rankedResults.Should().AllSatisfy(r => r.TopologicalScore.Should().BeGreaterThanOrEqualTo(0));
        rankedResults.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Context));
    }

    [Fact]
    public async Task ResultCache_CachesCommunityResults()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;
        _engine.ClearCache();

        // Act
        var firstResults = await _engine.SearchAsync(queryEmbedding, topK: 3, includeCommunities: true);
        var firstStats = _engine.GetCacheStatistics();
        var secondResults = await _engine.SearchAsync(queryEmbedding, topK: 3, includeCommunities: true);
        var secondStats = _engine.GetCacheStatistics();

        // Assert
        firstResults.Should().BeEquivalentTo(secondResults);
        firstStats.totalEntries.Should().BeGreaterThan(0);
        secondStats.totalEntries.Should().Be(firstStats.totalEntries);
    }

    [Fact]
    public async Task PerformanceProfiler_ProfileSearchAsync_ReturnsMetrics()
    {
        // Arrange
        var queryEmbedding = VectorSearchIntegration.GenerateMockEmbeddings([1], _embeddingDimensions)[0].Embedding;

        // Act
        var metrics = await PerformanceProfiler.ProfileSearchAsync(_engine, queryEmbedding, topK: 3, iterations: 2);

        // Assert
        metrics.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        metrics.NodesProcessed.Should().Be(3);
        metrics.OperationsPerSecond.Should().BeGreaterThan(0);
        metrics.OperationName.Should().Contain("GraphRAG Search");
    }

    [Fact]
    public async Task PerformanceProfiler_ProfileCommunityDetectionAsync_ReturnsMetrics()
    {
        // Act
        var metrics = await PerformanceProfiler.ProfileCommunityDetectionAsync(
            _database, _graphTable, "louvain", iterations: 2);

        // Assert
        metrics.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        metrics.NodesProcessed.Should().Be(5); // Our test graph has 5 nodes
        metrics.EdgesProcessed.Should().BeGreaterThan(0);
        metrics.OperationName.Should().Contain("Community Detection");
    }

    [Fact]
    public async Task MemoryOptimizer_ProcessInBatchesAsync_ProcessesCorrectly()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).ToList();
        var processedBatches = new List<List<int>>();

        // Act
        await MemoryOptimizer.ProcessInBatchesAsync(
            items,
            batchSize: 3,
            processor: async (batch, ct) =>
            {
                processedBatches.Add(batch.ToList());
                await Task.Delay(1, ct); // Simulate async work
            });

        // Assert
        processedBatches.Should().HaveCount(4); // 10 items / 3 batch size = 4 batches (3 + 3 + 3 + 1)
        processedBatches[0].Should().HaveCount(3);
        processedBatches[1].Should().HaveCount(3);
        processedBatches[2].Should().HaveCount(3);
        processedBatches[3].Should().HaveCount(1);
        processedBatches.SelectMany(b => b).Should().BeEquivalentTo(items);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _database?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

// Extension method to access cache (for testing)
internal static class GraphRagEngineExtensions
{
    public static ResultCache GetCache(this GraphRagEngine engine)
    {
        return (ResultCache)typeof(GraphRagEngine)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(engine)!;
    }
}
