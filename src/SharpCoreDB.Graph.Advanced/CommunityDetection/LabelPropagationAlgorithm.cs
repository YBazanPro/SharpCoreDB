#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.CommunityDetection;

/// <summary>
/// Label Propagation Algorithm for community detection.
/// Simple, scalable algorithm where labels propagate through the network.
/// 
/// Time Complexity: O(d * m) where d = diameter, m = edges
/// Space Complexity: O(n)
/// 
/// Reference: Raghavan et al., "Near linear time algorithm to detect community structures", 2007
/// </summary>
public sealed class LabelPropagationAlgorithm : ICommunityDetector
{
    private ExecutionMetrics? _lastMetrics;
    private const int MaxIterations = 100;
    private readonly Random _random = new(42); // Deterministic seed

    /// <summary>
    /// Gets the algorithm name.
    /// </summary>
    public string AlgorithmName => "Label Propagation";

    /// <summary>
    /// Gets metrics from the last execution.
    /// </summary>
    public ExecutionMetrics? LastExecutionMetrics => _lastMetrics;

    /// <summary>
    /// Executes label propagation for community detection.
    /// Each node adopts the most frequent label among its neighbors.
    /// </summary>
    public async Task<CommunityDetectionResult> ExecuteAsync(GraphData graphData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graphData);

        var sw = Stopwatch.StartNew();

        // Initialize: each node has unique label
        var labels = Enumerable.Range(0, graphData.NodeCount)
            .Select(i => (ulong)i)
            .ToArray();

        int iterations = 0;
        bool converged = false;

        // Propagate labels until convergence
        while (iterations < MaxIterations && !converged)
        {
            cancellationToken.ThrowIfCancellationRequested();

            converged = true;

            // Random node order (standard LPA practice)
            var nodeOrder = Enumerable.Range(0, graphData.NodeCount).OrderBy(_ => _random.Next()).ToArray();

            foreach (int nodeIdx in nodeOrder)
            {
                // Count neighbor labels
                var labelCounts = new Dictionary<ulong, int>();
                int degree = graphData.AdjacencyList[nodeIdx].Length;

                if (degree == 0) continue; // Isolated node

                foreach (var neighborIdx in graphData.AdjacencyList[nodeIdx])
                {
                    var label = labels[neighborIdx];
                    if (!labelCounts.ContainsKey(label))
                        labelCounts[label] = 0;
                    labelCounts[label]++;
                }

                // Adopt most frequent label (break ties randomly)
                var maxCount = labelCounts.Values.Max();
                var mostFrequent = labelCounts
                    .Where(kvp => kvp.Value == maxCount)
                    .Select(kvp => kvp.Key)
                    .OrderBy(_ => _random.Next())
                    .First();

                if (mostFrequent != labels[nodeIdx])
                {
                    labels[nodeIdx] = mostFrequent;
                    converged = false;
                }
            }

            iterations++;
        }

        sw.Stop();

        // Build results from final label assignment
        var communityGroups = labels
            .Select((label, nodeIdx) => (NodeId: graphData.NodeIds[nodeIdx], Label: label))
            .GroupBy(x => x.Label)
            .Select((group, idx) => new Community
            {
                Id = (ulong)idx,
                Members = group.Select(x => x.NodeId).ToList(),
                Density = CalculateCommunityDensity(graphData, labels, group.Key)
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
                { "num_communities", communityGroups.Length },
                { "converged", converged }
            }
        };

        return new CommunityDetectionResult
        {
            Communities = communityGroups,
            ConvergenceIterations = iterations,
            QualityMetrics = new Dictionary<string, object>
            {
                { "num_communities", communityGroups.Length },
                { "converged", converged },
                { "avg_community_size", graphData.NodeCount / (double)communityGroups.Length }
            }
        };
    }

    /// <summary>
    /// Calculates density for a community.
    /// </summary>
    private static double CalculateCommunityDensity(GraphData graphData, ulong[] labels, ulong targetLabel)
    {
        var nodes = labels
            .Select((label, idx) => (label, idx))
            .Where(x => x.label == targetLabel)
            .Select(x => x.idx)
            .ToArray();

        if (nodes.Length <= 1) return 0.0;

        int internalEdges = 0;
        foreach (var u in nodes)
        {
            foreach (var v in graphData.AdjacencyList[u])
            {
                if (Array.Exists(nodes, n => n == v) && u < v) // Count each edge once
                    internalEdges++;
            }
        }

        int maxEdges = nodes.Length * (nodes.Length - 1) / 2;
        return maxEdges > 0 ? internalEdges / (double)maxEdges : 0.0;
    }
}
