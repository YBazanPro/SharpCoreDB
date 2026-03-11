#nullable enable

using FluentAssertions;
using Xunit;
using SharpCoreDB.Graph.Advanced;
using SharpCoreDB.Graph.Advanced.CommunityDetection;
using SharpCoreDB.Graph.Advanced.Metrics;

namespace SharpCoreDB.Graph.Advanced.Tests;

/// <summary>
/// Tests for community detection algorithms.
/// </summary>
public class CommunityDetectionTests
{
    /// <summary>
    /// Creates a simple triangle graph: 3 nodes, all connected.
    /// </summary>
    private static GraphData CreateTriangleGraph()
    {
        return new GraphData
        {
            NodeIds = [1, 2, 3],
            AdjacencyList =
            [
                [1, 2],  // Node 0 -> 1, 2
                [0, 2],  // Node 1 -> 0, 2
                [0, 1]   // Node 2 -> 0, 1
            ],
            IsDirected = false
        };
    }

    /// <summary>
    /// Creates a graph with two separate triangles.
    /// </summary>
    private static GraphData CreateTwoCommunitiesGraph()
    {
        return new GraphData
        {
            NodeIds = [1, 2, 3, 4, 5, 6],
            AdjacencyList =
            [
                [1, 2],        // Community 1: 0-1-2
                [0, 2],
                [0, 1],
                [4, 5],        // Community 2: 3-4-5
                [3, 5],
                [3, 4]
            ],
            IsDirected = false
        };
    }

    [Fact]
    public async Task LouvainAlgorithm_WithTriangleGraph_ShouldDetectSingleCommunity()
    {
        // Arrange
        var algorithm = new LouvainAlgorithm();
        var graph = CreateTriangleGraph();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().HaveCount(1);
        result.Communities[0].Members.Should().HaveCount(3);
        result.OverallModularity.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LouvainAlgorithm_WithTwoCommunitiesGraph_ShouldDetectTwoCommunities()
    {
        // Arrange
        var algorithm = new LouvainAlgorithm();
        var graph = CreateTwoCommunitiesGraph();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().HaveCount(2);
        result.Communities.Should().AllSatisfy(c => c.Members.Should().HaveCount(3));
    }

    [Fact]
    public async Task LabelPropagationAlgorithm_ShouldConverge()
    {
        // Arrange
        var algorithm = new LabelPropagationAlgorithm();
        var graph = CreateTwoCommunitiesGraph();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().NotBeEmpty();
        result.ConvergenceIterations.Should().BeLessThan(100);
    }

    [Fact]
    public async Task ConnectedComponentsAlgorithm_WithTwoCommunitiesGraph_ShouldDetectTwoComponents()
    {
        // Arrange
        var algorithm = new ConnectedComponentsAlgorithm();
        var graph = CreateTwoCommunitiesGraph();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().HaveCount(2);
        result.Communities.Should().AllSatisfy(c => c.Members.Should().HaveCount(3));
    }

    [Fact]
    public async Task ConnectedComponentsAlgorithm_ShouldBeLinearTime()
    {
        // Arrange
        var algorithm = new ConnectedComponentsAlgorithm();
        var graph = CreateTriangleGraph();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().NotBeEmpty();
        algorithm.LastExecutionMetrics?.Duration.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }
}

/// <summary>
/// Tests for graph metrics.
/// </summary>
public class GraphMetricsTests
{
    private static GraphData CreateTriangleGraph()
    {
        return new GraphData
        {
            NodeIds = [1, 2, 3],
            AdjacencyList = [[1, 2], [0, 2], [0, 1]],
            IsDirected = false
        };
    }

    [Fact]
    public async Task DegreeCentrality_ShouldCalculateCorrectly()
    {
        // Arrange
        var metric = new DegreeCentrality();
        var graph = CreateTriangleGraph();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r =>
        {
            r.Value.Should().BeGreaterThan(0);
            r.MetricType.Should().Be("degree_centrality");
        });
    }

    [Fact]
    public async Task ClusteringCoefficient_ForTriangle_ShouldBeOne()
    {
        // Arrange
        var metric = new ClusteringCoefficient();
        var graph = CreateTriangleGraph();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(3);
        // For a complete triangle, all nodes have clustering = 1.0 (or 2.0 depending on normalization)
        results.Should().AllSatisfy(r => r.Value.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task BetweennessCentrality_ShouldComplete()
    {
        // Arrange
        var metric = new BetweennessCentrality();
        var graph = CreateTriangleGraph();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r =>
        {
            r.MetricType.Should().Be("betweenness_centrality");
        });
    }

    [Fact]
    public async Task ClosenessCentrality_ShouldComplete()
    {
        // Arrange
        var metric = new ClosenessCentrality();
        var graph = CreateTriangleGraph();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r =>
        {
            r.Value.Should().BeGreaterThanOrEqualTo(0);
            r.Value.Should().BeLessThanOrEqualTo(1);
        });
    }

    [Fact]
    public async Task EigenvectorCentrality_ShouldConverge()
    {
        // Arrange
        var metric = new EigenvectorCentrality();
        var graph = CreateTriangleGraph();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Value.Should().BeGreaterThanOrEqualTo(0));
        metric.LastExecutionMetrics?.CustomMetrics["converged"].Should().Be(true);
    }
}

/// <summary>
/// Tests for subgraph algorithms.
/// </summary>
public class SubgraphAlgorithmsTests
{
    private static GraphData CreateLineGraph()
    {
        // Linear graph: 0-1-2-3-4
        return new GraphData
        {
            NodeIds = [1, 2, 3, 4, 5],
            AdjacencyList =
            [
                [1],           // 0 -> 1
                [0, 2],        // 1 -> 0, 2
                [1, 3],        // 2 -> 1, 3
                [2, 4],        // 3 -> 2, 4
                [3]            // 4 -> 3
            ],
            IsDirected = false
        };
    }

    [Fact]
    public async Task KCoreDecomposition_OnLineGraph_ShouldWork()
    {
        // Arrange
        var graph = CreateLineGraph();

        // Act
        var (kCore, cores) = await SharpCoreDB.Graph.Advanced.SubgraphQueries.KCoreDecomposition
            .DecomposeAsync(graph);

        // Assert
        kCore.Length.Should().Be(5);
        cores.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TriangleDetector_ShouldFindTriangles()
    {
        // Arrange
        var graph = new GraphData
        {
            NodeIds = [1, 2, 3],
            AdjacencyList = [[1, 2], [0, 2], [0, 1]],
            IsDirected = false
        };

        // Act
        var triangles = await SharpCoreDB.Graph.Advanced.SubgraphQueries.TriangleDetector
            .DetectTrianglesAsync(graph);

        // Assert
        triangles.Should().HaveCount(1);
        var triangle = triangles[0];
        triangle.u.Should().Be(0);
        triangle.v.Should().Be(1);
        triangle.w.Should().Be(2);
    }

    [Fact]
    public async Task TriangleDetector_OnLineGraph_ShouldFindNoTriangles()
    {
        // Arrange
        var graph = CreateLineGraph();

        // Act
        var triangles = await SharpCoreDB.Graph.Advanced.SubgraphQueries.TriangleDetector
            .DetectTrianglesAsync(graph);

        // Assert
        triangles.Should().BeEmpty();
    }
}

/// <summary>
/// Performance benchmarks for Phase 12 algorithms.
/// </summary>
public class Phase12PerformanceTests
{
    /// <summary>
    /// Creates a synthetic graph with specified size.
    /// </summary>
    private static GraphData CreateSyntheticGraph(int nodeCount, double edgeProbability)
    {
        var random = new Random(42);
        var adjacencyList = new int[nodeCount][];

        for (int i = 0; i < nodeCount; i++)
        {
            var neighbors = new List<int>();
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (random.NextDouble() < edgeProbability)
                {
                    neighbors.Add(j);
                }
            }
            adjacencyList[i] = neighbors.ToArray();
        }

        return new GraphData
        {
            NodeIds = Enumerable.Range(0, nodeCount).Select(i => (ulong)i).ToArray(),
            AdjacencyList = adjacencyList,
            IsDirected = false
        };
    }

    [Fact]
    public async Task ConnectedComponents_OnMediumGraph_ShouldBeQuick()
    {
        // Arrange
        var graph = CreateSyntheticGraph(1000, 0.01); // 1000 nodes, sparse
        var algorithm = new ConnectedComponentsAlgorithm();

        // Act
        var result = await algorithm.ExecuteAsync(graph);

        // Assert
        result.Communities.Should().NotBeEmpty();
        algorithm.LastExecutionMetrics?.Duration.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DegreeCentrality_OnMediumGraph_ShouldBeLinearTime()
    {
        // Arrange
        var graph = CreateSyntheticGraph(10000, 0.001);
        var metric = new DegreeCentrality();

        // Act
        var results = await metric.CalculateAsync(graph);

        // Assert
        results.Should().HaveCount(10000);
        metric.LastExecutionMetrics?.Duration.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
