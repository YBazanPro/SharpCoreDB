#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.Metrics;

/// <summary>
/// Clustering Coefficient Calculation.
/// Measures how much neighbors of a node are connected to each other.
/// 
/// Time Complexity: O(n * d^2) where d = average degree
/// Space Complexity: O(n)
/// 
/// Local clustering: transitivity at each node
/// Global clustering: average or fraction over whole graph
/// Values range from 0 (no triangles) to 1 (complete local connectivity)
/// </summary>
public sealed class ClusteringCoefficient : IGraphMetric
{
    private ExecutionMetrics? _lastMetrics;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName => "Clustering Coefficient";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Calculates local clustering coefficient for all nodes.
    /// </summary>
    public async Task<GraphMetricResult[]> CalculateAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        var clustering = new double[graphData.NodeCount];

        // Calculate local clustering coefficient for each node
        for (int u = 0; u < graphData.NodeCount; u++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var neighbors = graphData.AdjacencyList[u];
            int degree = neighbors.Length;

            if (degree < 2)
            {
                clustering[u] = 0.0; // Can't have triangles with < 2 neighbors
                continue;
            }

            // Count edges between neighbors
            int triangles = 0;
            var neighborSet = new HashSet<int>(neighbors);

            for (int i = 0; i < neighbors.Length; i++)
            {
                int v = neighbors[i];
                // Count common neighbors (triangles)
                foreach (int w in graphData.AdjacencyList[v])
                {
                    if (neighborSet.Contains(w) && v < w)
                    {
                        triangles++;
                    }
                }
            }

            // Clustering coefficient: triangles / max possible
            int maxTriangles = degree * (degree - 1) / 2;
            clustering[u] = maxTriangles > 0 ? (2.0 * triangles) / maxTriangles : 0.0;
        }

        sw.Stop();

        var results = clustering
            .Select((value, idx) => new GraphMetricResult
            {
                NodeId = graphData.NodeIds[idx],
                Value = value,
                MetricType = "clustering_coefficient"
            })
            .ToArray();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = graphData.EdgeCount,
            PeakMemoryBytes = GC.GetTotalMemory(false),
            CustomMetrics = new Dictionary<string, object>
            {
                { "global_clustering_coefficient", results.Length > 0 ? results.Average(r => r.Value) : 0.0 }
            }
        };

        return results;
    }
}

/// <summary>
/// Eigenvector Centrality Calculation using Power Iteration.
/// Measures a node's influence based on its connections to other influential nodes.
/// 
/// Time Complexity: O(k(n + m)) where k = iterations
/// Space Complexity: O(n)
/// 
/// Useful for identifying important nodes in networks (like PageRank).
/// Reference: Newman, "Networks: An Introduction"
/// </summary>
public sealed class EigenvectorCentrality : IGraphMetric
{
    private ExecutionMetrics? _lastMetrics;
    private const int MaxIterations = 100;
    private const double Tolerance = 1e-6;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName => "Eigenvector Centrality";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Calculates eigenvector centrality using power iteration method.
    /// </summary>
    public async Task<GraphMetricResult[]> CalculateAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        // Initialize with uniform distribution
        var eigenvector = new double[graphData.NodeCount];
        for (int i = 0; i < graphData.NodeCount; i++)
            eigenvector[i] = 1.0 / graphData.NodeCount;

        var nextEigenvector = new double[graphData.NodeCount];
        double maxDiff = double.MaxValue;
        int iterations = 0;

        // Power iteration
        while (iterations < MaxIterations && maxDiff > Tolerance)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(nextEigenvector);

            // Multiply by adjacency matrix: next = A * current
            for (int u = 0; u < graphData.NodeCount; u++)
            {
                foreach (int v in graphData.AdjacencyList[u])
                {
                    nextEigenvector[u] += eigenvector[v];
                }
            }

            // Normalize
            double norm = 0.0;
            for (int i = 0; i < graphData.NodeCount; i++)
                norm += nextEigenvector[i] * nextEigenvector[i];

            norm = Math.Sqrt(norm);
            if (norm > 0)
            {
                for (int i = 0; i < graphData.NodeCount; i++)
                    nextEigenvector[i] /= norm;
            }

            // Check convergence
            maxDiff = 0.0;
            for (int i = 0; i < graphData.NodeCount; i++)
            {
                maxDiff = Math.Max(maxDiff, Math.Abs(nextEigenvector[i] - eigenvector[i]));
            }

            // Swap
            (eigenvector, nextEigenvector) = (nextEigenvector, eigenvector);
            iterations++;
        }

        sw.Stop();

        var results = eigenvector
            .Select((value, idx) => new GraphMetricResult
            {
                NodeId = graphData.NodeIds[idx],
                Value = Math.Max(0, value), // Ensure non-negative
                MetricType = "eigenvector_centrality"
            })
            .ToArray();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            Iterations = iterations,
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = graphData.EdgeCount,
            PeakMemoryBytes = GC.GetTotalMemory(false),
            CustomMetrics = new Dictionary<string, object>
            {
                { "converged", maxDiff <= Tolerance },
                { "iterations", iterations }
            }
        };

        return results;
    }
}
