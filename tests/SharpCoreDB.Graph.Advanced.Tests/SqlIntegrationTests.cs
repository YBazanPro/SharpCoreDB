#nullable enable

using FluentAssertions;
using Xunit;
using SharpCoreDB.Graph.Advanced.SqlIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreDB.Graph.Advanced.Tests;

/// <summary>
/// SQL integration tests for Phase 12 graph analytics.
/// Tests end-to-end SQL function execution with real database.
/// </summary>
public class SqlIntegrationTests : IDisposable
{
    private readonly Database _database;
    private readonly string _testTableName = "test_graph_edges";
    private bool _disposed;

    public SqlIntegrationTests()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection()
            .AddSharpCoreDB()
            .BuildServiceProvider();

        _database = new Database(
            services,
            Path.Combine(Path.GetTempPath(), $"test_graph_{Guid.NewGuid()}"),
            "test_password",
            isReadOnly: false);

        // Create test table with graph edges
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create table for graph edges
        _database.ExecuteSQL($@"
            CREATE TABLE {_testTableName} (
                source INTEGER,
                target INTEGER,
                weight REAL DEFAULT 1.0
            )
        ");

        // Insert triangle graph: nodes 1-2-3 all connected
        var edges = new[]
        {
            (1, 2), (2, 1), // Bidirectional
            (2, 3), (3, 2),
            (3, 1), (1, 3)
        };

        foreach (var (source, target) in edges)
        {
            _database.ExecuteSQL($@"
                INSERT INTO {_testTableName} (source, target) 
                VALUES ({source}, {target})
            ");
        }

        // Flush to ensure data is persisted
        _database.Flush();
    }

    [Fact]
    public async Task GraphLoader_ValidatesGraphTable_Correctly()
    {
        // Act
        var isValid = GraphLoader.ValidateGraphTable(_database, _testTableName);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task GraphLoader_LoadsTriangleGraph_Correctly()
    {
        // Act
        var graphData = await GraphLoader.LoadFromTableAsync(_database, _testTableName);

        // Assert
        graphData.NodeCount.Should().Be(3);
        graphData.EdgeCount.Should().Be(3); // Triangle has 3 edges
        graphData.NodeIds.Should().BeEquivalentTo(new[] { 1UL, 2UL, 3UL });
    }

    [Fact]
    public async Task CommunityDetectionFunctions_DetectsCommunities_InTriangle()
    {
        // Act
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _testTableName);

        // Assert
        communities.Should().HaveCount(3); // Each node in its own community (triangle is complete)
        communities.Select(c => c.communityId).Distinct().Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CommunityDetectionFunctions_GetConnectedComponents_FindsOneComponent()
    {
        // Act
        var components = await CommunityDetectionFunctions.GetConnectedComponentsAsync(_database, _testTableName);

        // Assert
        components.Should().HaveCount(3);
        components.Select(c => c.componentId).Distinct().Should().HaveCount(1); // All connected
    }

    [Fact]
    public async Task GraphMetricsFunctions_CalculatesDegreeCentrality()
    {
        // Act
        var degrees = await GraphMetricsFunctions.CalculateDegreeCentralityAsync(_database, _testTableName);

        // Assert
        degrees.Should().HaveCount(3);
        degrees.Should().AllSatisfy(d => d.degree.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GraphMetricsFunctions_CalculatesClusteringCoefficient()
    {
        // Act
        var coefficients = await GraphMetricsFunctions.CalculateClusteringCoefficientAsync(_database, _testTableName);

        // Assert
        coefficients.Should().HaveCount(3);
        coefficients.Should().AllSatisfy(c => c.coefficient.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    public async Task SubgraphFunctions_FindsTriangles()
    {
        // Act
        var triangles = await SubgraphFunctions.FindTrianglesAsync(_database, _testTableName);

        // Assert
        triangles.Should().HaveCount(1); // One triangle in the graph
        var triangle = triangles[0];
        var nodes = new[] { triangle.node1, triangle.node2, triangle.node3 };
        nodes.Should().BeEquivalentTo(new[] { 1UL, 2UL, 3UL });
    }

    [Fact]
    public async Task SubgraphFunctions_ExtractsSubgraph()
    {
        // Act - Extract subgraph from node 1 with depth 1
        var subgraph = await SubgraphFunctions.ExtractSubgraphAsync(_database, _testTableName, 1, 1);

        // Assert
        subgraph.Should().HaveCountGreaterThan(0);
        subgraph.Should().AllSatisfy(s => s.distance.Should().BeLessThanOrEqualTo(1));
    }

    [Fact]
    public async Task SubgraphFunctions_FindsKCores()
    {
        // Act - Find 2-cores (nodes with degree >= 2)
        var kCores = await SubgraphFunctions.GetKCoreAsync(_database, _testTableName, 2);

        // Assert
        kCores.Should().HaveCount(3); // All nodes in triangle have degree 2
        kCores.Should().AllSatisfy(k => k.k.Should().BeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task GraphRagFunctions_ProvidesCommunityContext()
    {
        // Act
        var context = await GraphRagFunctions.CommunitySematicContextAsync(_database, 1, _testTableName, 2);

        // Assert
        context.Should().HaveCountGreaterThan(0);
        context.Should().AllSatisfy(c => c.distance.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task CommunityDetectionFunctions_GetCommunityMembers_Works()
    {
        // First get communities
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _testTableName);
        var firstCommunityId = communities.First().communityId;

        // Act
        var members = CommunityDetectionFunctions.GetCommunityMembers(_database, _testTableName, firstCommunityId);

        // Assert
        members.Should().HaveCountGreaterThan(0);
        members.Should().AllSatisfy(m => communities.Any(c => c.nodeId == m && c.communityId == firstCommunityId));
    }

    [Fact]
    public async Task CommunityDetectionFunctions_GetCommunitySize_Works()
    {
        // First get communities
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(_database, _testTableName);
        var firstCommunityId = communities.First().communityId;

        // Act
        var size = CommunityDetectionFunctions.GetCommunitySize(_database, _testTableName, firstCommunityId);

        // Assert
        size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GraphMetricsFunctions_CalculatesGlobalClusteringCoefficient()
    {
        // Act
        var globalCoeff = await GraphMetricsFunctions.CalculateGlobalClusteringCoefficientAsync(_database, _testTableName);

        // Assert
        globalCoeff.Should().BeGreaterThanOrEqualTo(0);
        globalCoeff.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task SubgraphFunctions_FindsCliques_InTriangle()
    {
        // Act
        var cliques = await SubgraphFunctions.FindCliquesAsync(_database, _testTableName, 3);

        // Assert
        cliques.Should().HaveCountGreaterThanOrEqualTo(1); // At least the full triangle
        cliques.Should().AllSatisfy(c => c.memberCount.Should().BeGreaterThanOrEqualTo(3));
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
