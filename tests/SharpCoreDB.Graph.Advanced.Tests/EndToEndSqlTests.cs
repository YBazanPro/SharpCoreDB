#nullable enable

using FluentAssertions;
using Xunit;
using SharpCoreDB.Graph.Advanced.SqlIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreDB.Graph.Advanced.Tests;

/// <summary>
/// End-to-end SQL query tests demonstrating real SQL execution.
/// </summary>
public class EndToEndSqlTests : IDisposable
{
    private readonly Database _database;
    private readonly string _graphTable = "social_graph";
    private bool _disposed;

    public EndToEndSqlTests()
    {
        // Create test database
        var services = new ServiceCollection()
            .AddSharpCoreDB()
            .BuildServiceProvider();

        _database = new Database(
            services,
            Path.Combine(Path.GetTempPath(), $"e2e_test_{Guid.NewGuid()}"),
            "test_password",
            isReadOnly: false);

        SetupSocialGraph();
    }

    private void SetupSocialGraph()
    {
        // Create a small social network graph
        _database.ExecuteSQL($@"
            CREATE TABLE {_graphTable} (
                source INTEGER,
                target INTEGER,
                relationship TEXT DEFAULT 'friend'
            )
        ");

        // Add edges representing friendships
        var friendships = new[]
        {
            (1, 2), (2, 1), // Alice <-> Bob
            (2, 3), (3, 2), // Bob <-> Charlie
            (3, 4), (4, 3), // Charlie <-> David
            (4, 5), (5, 4), // David <-> Eve
            (1, 3), (3, 1), // Alice <-> Charlie (triangle)
            (2, 4), (4, 2)  // Bob <-> David
        };

        foreach (var (source, target) in friendships)
        {
            _database.ExecuteSQL($@"
                INSERT INTO {_graphTable} (source, target) 
                VALUES ({source}, {target})
            ");
        }

        _database.Flush();
    }

    [Fact]
    public async Task EndToEnd_CommunityDetectionWorkflow()
    {
        // Act: Detect communities using SQL function
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _graphTable);

        // Assert: Should find connected components
        communities.Should().HaveCount(5); // All 5 nodes
        var communityIds = communities.Select(c => c.communityId).Distinct().ToList();
        communityIds.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task EndToEnd_MetricsCalculationWorkflow()
    {
        // Act: Calculate degree centrality
        var degrees = await GraphMetricsFunctions.CalculateDegreeCentralityAsync(_database, _graphTable);

        // Assert: All nodes should have degree > 0
        degrees.Should().HaveCount(5);
        degrees.Should().AllSatisfy(d => d.degree.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task EndToEnd_SubgraphAnalysisWorkflow()
    {
        // Act: Find triangles in the social graph
        var triangles = await SubgraphFunctions.FindTrianglesAsync(_database, _graphTable);

        // Assert: Should find the Alice-Bob-Charlie triangle
        triangles.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task EndToEnd_GraphRagWorkflow()
    {
        // Act: Get community context for a node
        var context = await GraphRagFunctions.CommunitySematicContextAsync(
            _database, 1, _graphTable, maxDistance: 2);

        // Assert: Should find related nodes
        context.Should().HaveCountGreaterThan(0);
        context.Should().AllSatisfy(c => c.distance.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task EndToEnd_CompleteGraphAnalysis()
    {
        // Perform complete graph analysis workflow

        // 1. Load and validate graph
        var graphData = await GraphLoader.LoadFromTableAsync(_database, _graphTable);
        graphData.NodeCount.Should().Be(5);
        graphData.EdgeCount.Should().Be(12); // 6 bidirectional edges

        // 2. Detect communities
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _graphTable);
        communities.Should().HaveCount(5);

        // 3. Calculate metrics
        var degrees = await GraphMetricsFunctions.CalculateDegreeCentralityAsync(_database, _graphTable);
        var clustering = await GraphMetricsFunctions.CalculateClusteringCoefficientAsync(_database, _graphTable);

        degrees.Should().HaveCount(5);
        clustering.Should().HaveCount(5);

        // 4. Find subgraphs
        var triangles = await SubgraphFunctions.FindTrianglesAsync(_database, _graphTable);
        var kCores = await SubgraphFunctions.GetKCoreAsync(_database, _graphTable, k: 2);

        triangles.Should().HaveCountGreaterThanOrEqualTo(1);
        kCores.Should().HaveCountGreaterThanOrEqualTo(3); // Most nodes have degree >= 2
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
