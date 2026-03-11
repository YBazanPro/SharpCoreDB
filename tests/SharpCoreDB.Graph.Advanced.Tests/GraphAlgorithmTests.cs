#nullable enable

using FluentAssertions;
using Xunit;
using SharpCoreDB.Graph.Advanced;

namespace SharpCoreDB.Graph.Advanced.Tests;

/// <summary>
/// Basic tests for graph algorithm infrastructure.
/// </summary>
public class GraphAlgorithmTests
{
    [Fact]
    public void GraphData_WithValidInput_ShouldInitializeCorrectly()
    {
        // Arrange
        var nodeIds = new[] { 1UL, 2UL, 3UL };
        var adjacencyList = new[]
        {
            new[] { 1, 2 },  // Node 0 connects to 1, 2
            new[] { 0, 2 },  // Node 1 connects to 0, 2
            new[] { 0, 1 }   // Node 2 connects to 0, 1 (triangle)
        };

        // Act
        var graph = new GraphData
        {
            NodeIds = nodeIds,
            AdjacencyList = adjacencyList,
            IsDirected = false
        };

        // Assert
        graph.NodeCount.Should().Be(3);
        graph.NodeIds.Should().HaveCount(3);
        graph.AdjacencyList.Should().HaveCount(3);
    }

    [Fact]
    public void GraphData_EdgeCount_ShouldCalculateCorrectly()
    {
        // Arrange - Create simple triangle graph (3 nodes, 3 edges)
        var nodeIds = new[] { 1UL, 2UL, 3UL };
        var adjacencyList = new[]
        {
            new[] { 1, 2 },
            new[] { 0, 2 },
            new[] { 0, 1 }
        };

        var graph = new GraphData
        {
            NodeIds = nodeIds,
            AdjacencyList = adjacencyList,
            IsDirected = false
        };

        // Act
        var edgeCount = graph.EdgeCount;

        // Assert
        edgeCount.Should().Be(3); // Triangle has 3 edges
    }

    [Fact]
    public void ExecutionMetrics_ShouldTrackPerformanceData()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            Duration = TimeSpan.FromSeconds(1.5),
            Iterations = 5,
            PeakMemoryBytes = 1024 * 1024, // 1 MB
            NodesProcessed = 1000,
            EdgesProcessed = 5000
        };

        // Act & Assert
        metrics.Duration.Should().Be(TimeSpan.FromSeconds(1.5));
        metrics.Iterations.Should().Be(5);
        metrics.PeakMemoryBytes.Should().Be(1048576);
        metrics.NodesProcessed.Should().Be(1000);
    }

    [Fact]
    public void Community_ShouldRepresentNodeCluster()
    {
        // Arrange
        var members = new List<ulong> { 1, 2, 3, 4, 5 };
        var community = new Community
        {
            Id = 1,
            Members = members,
            Modularity = 0.75,
            Density = 0.8
        };

        // Act & Assert
        community.Id.Should().Be(1);
        community.Members.Should().HaveCount(5);
        community.Modularity.Should().Be(0.75);
        community.Density.Should().Be(0.8);
    }

    [Fact]
    public void GraphMetricResult_ShouldStoreNodeMetrics()
    {
        // Arrange
        var result = new GraphMetricResult
        {
            NodeId = 42,
            Value = 0.5,
            MetricType = "betweenness_centrality"
        };

        // Act & Assert
        result.NodeId.Should().Be(42);
        result.Value.Should().Be(0.5);
        result.MetricType.Should().Be("betweenness_centrality");
    }
}
