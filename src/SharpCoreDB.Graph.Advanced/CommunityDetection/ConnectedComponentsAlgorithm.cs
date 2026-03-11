#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.CommunityDetection;

/// <summary>
/// Connected Components Algorithm using Union-Find with path compression.
/// Finds connected components in an undirected graph.
/// 
/// Time Complexity: O(n * α(n)) where α = inverse Ackermann function (near-constant)
/// Space Complexity: O(n)
/// 
/// This is the fastest method for detecting connected components in a graph.
/// </summary>
public sealed class ConnectedComponentsAlgorithm : ICommunityDetector
{
    private ExecutionMetrics? _lastMetrics;

    /// <summary>
    /// Gets the algorithm name.
    /// </summary>
    public string AlgorithmName => "Connected Components (Union-Find)";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Finds connected components using Union-Find algorithm.
    /// </summary>
    public async Task<CommunityDetectionResult> ExecuteAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        // Union-Find data structure
        var parent = Enumerable.Range(0, graphData.NodeCount).ToArray();
        var rank = new int[graphData.NodeCount];

        // Process edges: union connected nodes
        var processedEdges = new HashSet<(int, int)>();

        for (int u = 0; u < graphData.NodeCount; u++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var v in graphData.AdjacencyList[u])
            {
                // Skip if already processed (avoid duplicate edges)
                int minIdx = Math.Min(u, v);
                int maxIdx = Math.Max(u, v);
                if (processedEdges.Contains((minIdx, maxIdx)))
                    continue;

                processedEdges.Add((minIdx, maxIdx));

                // Union the two nodes
                Union(parent, rank, u, v);
            }
        }

        sw.Stop();

        // Find all unique components
        var componentMap = new Dictionary<int, List<int>>();
        for (int i = 0; i < graphData.NodeCount; i++)
        {
            int root = Find(parent, i);
            if (!componentMap.ContainsKey(root))
                componentMap[root] = [];
            componentMap[root].Add(i);
        }

        // Build results
        var communities = componentMap
            .Select((kvp, idx) => new Community
            {
                Id = (ulong)idx,
                Members = kvp.Value.Select(nodeIdx => graphData.NodeIds[nodeIdx]).ToList(),
                Density = CalculateDensity(graphData, kvp.Value)
            })
            .ToArray();

        _lastMetrics = new ExecutionMetrics
        {
            Duration = sw.Elapsed,
            Iterations = 1, // Union-Find is single-pass
            NodesProcessed = graphData.NodeCount,
            EdgesProcessed = processedEdges.Count,
            PeakMemoryBytes = GC.GetTotalMemory(false),
            CustomMetrics = new Dictionary<string, object>
            {
                { "num_components", communities.Length }
            }
        };

        return new CommunityDetectionResult
        {
            Communities = communities,
            ConvergenceIterations = 1,
            QualityMetrics = new Dictionary<string, object>
            {
                { "num_components", communities.Length },
                { "largest_component_size", communities.Length > 0 ? communities.Max(c => c.Members.Count) : 0 }
            }
        };
    }

    /// <summary>
    /// Find operation with path compression.
    /// </summary>
    private static int Find(int[] parent, int x)
    {
        if (parent[x] != x)
        {
            parent[x] = Find(parent, parent[x]); // Path compression
        }
        return parent[x];
    }

    /// <summary>
    /// Union operation with union by rank.
    /// </summary>
    private static void Union(int[] parent, int[] rank, int x, int y)
    {
        int rootX = Find(parent, x);
        int rootY = Find(parent, y);

        if (rootX == rootY)
            return; // Already in same set

        // Union by rank
        if (rank[rootX] < rank[rootY])
        {
            parent[rootX] = rootY;
        }
        else if (rank[rootX] > rank[rootY])
        {
            parent[rootY] = rootX;
        }
        else
        {
            parent[rootY] = rootX;
            rank[rootX]++;
        }
    }

    /// <summary>
    /// Calculates density for a component.
    /// </summary>
    private static double CalculateDensity(GraphData graphData, List<int> nodeIndices)
    {
        if (nodeIndices.Count <= 1)
            return 0.0;

        int internalEdges = 0;
        var nodeSet = new HashSet<int>(nodeIndices);

        foreach (var u in nodeIndices)
        {
            foreach (var v in graphData.AdjacencyList[u])
            {
                if (nodeSet.Contains(v) && u < v)
                    internalEdges++;
            }
        }

        int n = nodeIndices.Count;
        int maxEdges = n * (n - 1) / 2;
        return maxEdges > 0 ? internalEdges / (double)maxEdges : 0.0;
    }
}
