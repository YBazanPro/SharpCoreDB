#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.Metrics;

/// <summary>
/// Betweenness Centrality Calculation using Brandes' algorithm.
/// Measures how often a node lies on the shortest path between other nodes.
/// 
/// Time Complexity: O(n + m) per source, O(n(n + m)) total
/// Space Complexity: O(n + m)
/// 
/// High values indicate nodes that are "bridges" in the network.
/// Reference: Brandes, "A faster algorithm for betweenness centrality", 2001
/// </summary>
public sealed class BetweennessCentrality : IGraphMetric
{
    private ExecutionMetrics? _lastMetrics;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName => "Betweenness Centrality";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Calculates betweenness centrality for all nodes using Brandes' algorithm.
    /// </summary>
    public async Task<GraphMetricResult[]> CalculateAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        var betweenness = new double[graphData.NodeCount];
        int processedCount = 0;

        // Process each node as source for shortest path calculation
        for (int source = 0; source < graphData.NodeCount; source++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // BFS from source
            var predecessors = new List<int>[graphData.NodeCount];
            var distance = new int[graphData.NodeCount];
            var dependency = new double[graphData.NodeCount];

            for (int i = 0; i < graphData.NodeCount; i++)
            {
                predecessors[i] = [];
                distance[i] = -1;
            }

            distance[source] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(source);

            // Forward BFS
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in graphData.AdjacencyList[u])
                {
                    if (distance[v] < 0)
                    {
                        distance[v] = distance[u] + 1;
                        queue.Enqueue(v);
                    }

                    if (distance[v] == distance[u] + 1)
                    {
                        predecessors[v].Add(u);
                    }
                }
            }

            // Backward accumulation (in reverse order of distance)
            var stack = new Stack<int>();
            for (int i = 0; i < graphData.NodeCount; i++)
            {
                if (distance[i] >= 0)
                    stack.Push(i);
            }

            while (stack.Count > 0)
            {
                int w = stack.Pop();
                foreach (int v in predecessors[w])
                {
                    dependency[v] += (1.0 + dependency[w]) * predecessors[w].Count / predecessors[w].Count;
                }

                if (w != source)
                {
                    betweenness[w] += dependency[w];
                }
            }

            processedCount++;
        }

        sw.Stop();

        // Normalize: divide by (n-1)(n-2)/2
        double normalization = 2.0 / ((graphData.NodeCount - 1) * (graphData.NodeCount - 2));
        var results = betweenness
            .Select((value, idx) => new GraphMetricResult
            {
                NodeId = graphData.NodeIds[idx],
                Value = value * normalization,
                MetricType = "betweenness_centrality"
            })
            .ToArray();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = graphData.EdgeCount,
            PeakMemoryBytes = GC.GetTotalMemory(false)
        };

        return results;
    }
}

/// <summary>
/// Closeness Centrality Calculation.
/// Measures how close a node is to all other nodes (average shortest path).
/// 
/// Time Complexity: O(n(n + m)) using BFS
/// Space Complexity: O(n + m)
/// 
/// High values indicate central nodes with short distances to others.
/// </summary>
public sealed class ClosenessCentrality : IGraphMetric
{
    private ExecutionMetrics? _lastMetrics;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName => "Closeness Centrality";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Calculates closeness centrality for all nodes.
    /// </summary>
    public async Task<GraphMetricResult[]> CalculateAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        var closeness = new double[graphData.NodeCount];

        // BFS from each node
        for (int source = 0; source < graphData.NodeCount; source++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var distance = new int[graphData.NodeCount];
            for (int i = 0; i < graphData.NodeCount; i++)
                distance[i] = -1;

            distance[source] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(source);

            int reachableCount = 1;
            long totalDistance = 0;

            // BFS
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                foreach (int v in graphData.AdjacencyList[u])
                {
                    if (distance[v] < 0)
                    {
                        distance[v] = distance[u] + 1;
                        totalDistance += distance[v];
                        reachableCount++;
                        queue.Enqueue(v);
                    }
                }
            }

            // Closeness: reachable nodes / sum of distances
            if (reachableCount > 1 && totalDistance > 0)
            {
                closeness[source] = (reachableCount - 1) / (double)totalDistance;
            }
        }

        sw.Stop();

        var results = closeness
            .Select((value, idx) => new GraphMetricResult
            {
                NodeId = graphData.NodeIds[idx],
                Value = value,
                MetricType = "closeness_centrality"
            })
            .ToArray();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = graphData.EdgeCount,
            PeakMemoryBytes = GC.GetTotalMemory(false)
        };

        return results;
    }
}

/// <summary>
/// Degree Centrality Calculation.
/// Simplest centrality measure: count of direct connections.
/// 
/// Time Complexity: O(n)
/// Space Complexity: O(n)
/// 
/// Fast baseline measure of node connectivity.
/// </summary>
public sealed class DegreeCentrality : IGraphMetric
{
    private ExecutionMetrics? _lastMetrics;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName => "Degree Centrality";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Calculates degree centrality (normalized) for all nodes.
    /// </summary>
    public async Task<GraphMetricResult[]> CalculateAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        // Normalize by max possible degree
        double normalization = graphData.NodeCount > 1 ? 1.0 / (graphData.NodeCount - 1) : 1.0;

        var results = graphData.AdjacencyList
            .Select((neighbors, idx) => new GraphMetricResult
            {
                NodeId = graphData.NodeIds[idx],
                Value = neighbors.Length * normalization,
                MetricType = "degree_centrality"
            })
            .ToArray();

        sw.Stop();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = graphData.EdgeCount,
            PeakMemoryBytes = GC.GetTotalMemory(false)
        };

        return results;
    }
}
