#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.GraphRAG;

/// <summary>
/// Enhanced ranking algorithms for GraphRAG that combine semantic similarity
/// with topological and community-based factors.
/// </summary>
public static class EnhancedRanking
{
    /// <summary>
    /// Result of an enhanced ranking operation.
    /// </summary>
    public readonly record struct RankedResult(
        ulong NodeId,
        double SemanticScore,
        double TopologicalScore,
        double CommunityScore,
        double CombinedScore,
        string Context
    );

    /// <summary>
    /// Performs enhanced ranking combining multiple factors.
    /// </summary>
    /// <param name="semanticResults">Results from semantic similarity search.</param>
    /// <param name="graphData">Graph structure data.</param>
    /// <param name="communities">Community detection results.</param>
    /// <param name="queryNode">The node from which the query originated (optional).</param>
    /// <param name="weights">Weights for combining different scores (semantic, topological, community).</param>
    /// <returns>Ranked results sorted by combined score.</returns>
    public static List<RankedResult> RankResults(
        List<(ulong nodeId, double semanticScore)> semanticResults,
        GraphData graphData,
        List<(ulong nodeId, ulong communityId)> communities,
        ulong? queryNode = null,
        (double semantic, double topological, double community) weights = default)
    {
        ArgumentNullException.ThrowIfNull(semanticResults);
        ArgumentNullException.ThrowIfNull(graphData);
        ArgumentNullException.ThrowIfNull(communities);

        // Default weights if not specified
        if (weights == default)
        {
            weights = (semantic: 0.5, topological: 0.3, community: 0.2);
        }

        // Validate weights sum to 1.0
        var totalWeight = weights.semantic + weights.topological + weights.community;
        if (Math.Abs(totalWeight - 1.0) > 0.001)
        {
            throw new ArgumentException("Weights must sum to 1.0");
        }

        // Create lookup dictionaries for fast access
        var communityLookup = communities.ToDictionary(x => x.nodeId, x => x.communityId);
        var nodeIndexLookup = graphData.NodeIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        // Get query node community if specified
        ulong? queryCommunity = queryNode.HasValue && communityLookup.TryGetValue(queryNode.Value, out var qc) ? qc : null;

        var rankedResults = new List<RankedResult>();

        foreach (var (nodeId, semanticScore) in semanticResults)
        {
            // Skip if node not in graph
            if (!nodeIndexLookup.TryGetValue(nodeId, out var nodeIndex))
            {
                continue;
            }

            // Calculate topological score (based on degree centrality)
            var degree = graphData.AdjacencyList[nodeIndex].Length;
            var maxDegree = graphData.AdjacencyList.Max(list => list.Length);
            var topologicalScore = maxDegree > 0 ? (double)degree / maxDegree : 0.0;

            // Calculate community score
            var communityScore = 0.0;
            if (communityLookup.TryGetValue(nodeId, out var nodeCommunity))
            {
                if (queryCommunity.HasValue && nodeCommunity == queryCommunity.Value)
                {
                    // Same community as query node - high score
                    communityScore = 1.0;
                }
                else
                {
                    // Different community - score based on community size
                    var communitySize = communities.Count(c => c.communityId == nodeCommunity);
                    var totalNodes = communities.Count;
                    communityScore = communitySize > 0 ? (double)communitySize / totalNodes : 0.0;
                }
            }

            // Calculate combined score
            var combinedScore = 
                weights.semantic * semanticScore +
                weights.topological * topologicalScore +
                weights.community * communityScore;

            // Generate context description
            var context = GenerateContextDescription(
                nodeId, semanticScore, topologicalScore, communityScore, 
                nodeCommunity, queryCommunity, degree);

            rankedResults.Add(new RankedResult(
                NodeId: nodeId,
                SemanticScore: semanticScore,
                TopologicalScore: topologicalScore,
                CommunityScore: communityScore,
                CombinedScore: combinedScore,
                Context: context
            ));
        }

        // Sort by combined score (descending)
        return rankedResults
            .OrderByDescending(r => r.CombinedScore)
            .ToList();
    }

    /// <summary>
    /// Performs multi-hop ranking that considers paths through the graph.
    /// </summary>
    /// <param name="semanticResults">Initial semantic search results.</param>
    /// <param name="graphData">Graph structure data.</param>
    /// <param name="communities">Community detection results.</param>
    /// <param name="maxHops">Maximum number of hops to consider.</param>
    /// <param name="queryNode">The node from which the query originated.</param>
    /// <returns>Enhanced ranked results considering multi-hop connections.</returns>
    public static List<RankedResult> RankWithMultiHop(
        List<(ulong nodeId, double semanticScore)> semanticResults,
        GraphData graphData,
        List<(ulong nodeId, ulong communityId)> communities,
        int maxHops,
        ulong queryNode)
    {
        ArgumentNullException.ThrowIfNull(semanticResults);
        ArgumentNullException.ThrowIfNull(graphData);
        ArgumentNullException.ThrowIfNull(communities);
        if (maxHops < 1) throw new ArgumentOutOfRangeException(nameof(maxHops));

        var nodeIndexLookup = graphData.NodeIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        // Find query node index
        if (!nodeIndexLookup.TryGetValue(queryNode, out var queryIndex))
        {
            // Query node not in graph, fall back to basic ranking
            return RankResults(semanticResults, graphData, communities, queryNode);
        }

        // Perform BFS to find nodes within maxHops
        var visited = new bool[graphData.NodeCount];
        var distances = new int[graphData.NodeCount];
        var queue = new Queue<int>();

        Array.Fill(distances, -1);
        distances[queryIndex] = 0;
        visited[queryIndex] = true;
        queue.Enqueue(queryIndex);

        var nodesWithinHops = new HashSet<ulong> { queryNode };

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var currentDistance = distances[currentIndex];

            if (currentDistance >= maxHops) break;

            foreach (var neighborIndex in graphData.AdjacencyList[currentIndex])
            {
                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    distances[neighborIndex] = currentDistance + 1;
                    nodesWithinHops.Add(graphData.NodeIds[neighborIndex]);
                    queue.Enqueue(neighborIndex);
                }
            }
        }

        // Boost scores for nodes within hop distance
        var enhancedResults = new List<(ulong nodeId, double semanticScore)>();

        foreach (var (nodeId, semanticScore) in semanticResults)
        {
            var enhancedScore = semanticScore;

            // Boost score if node is within hop distance
            if (nodesWithinHops.Contains(nodeId))
            {
                var distance = distances[nodeIndexLookup[nodeId]];
                var hopBoost = 1.0 + (maxHops - distance) * 0.1; // Closer nodes get more boost
                enhancedScore *= hopBoost;
            }

            enhancedResults.Add((nodeId, Math.Min(enhancedScore, 1.0))); // Cap at 1.0
        }

        return RankResults(enhancedResults, graphData, communities, queryNode);
    }

    /// <summary>
    /// Performs temporal ranking that considers recency and frequency.
    /// </summary>
    /// <param name="semanticResults">Semantic search results.</param>
    /// <param name="temporalData">Temporal information for each node (last accessed, access count).</param>
    /// <param name="recencyWeight">Weight for recency factor.</param>
    /// <param name="frequencyWeight">Weight for frequency factor.</param>
    /// <returns>Results ranked with temporal factors.</returns>
    public static List<(ulong nodeId, double score, string temporalContext)> RankWithTemporal(
        List<(ulong nodeId, double semanticScore)> semanticResults,
        Dictionary<ulong, (DateTime lastAccessed, int accessCount)> temporalData,
        double recencyWeight = 0.3,
        double frequencyWeight = 0.2)
    {
        ArgumentNullException.ThrowIfNull(semanticResults);
        ArgumentNullException.ThrowIfNull(temporalData);

        var now = DateTime.UtcNow;
        var maxAge = TimeSpan.FromDays(30); // Consider 30 days as max age

        var results = new List<(ulong nodeId, double score, string temporalContext)>();

        foreach (var (nodeId, semanticScore) in semanticResults)
        {
            var temporalScore = 0.0;
            var temporalContext = "No temporal data";

            if (temporalData.TryGetValue(nodeId, out var temporal))
            {
                // Recency score (newer = higher score)
                var age = now - temporal.lastAccessed;
                var recencyScore = Math.Max(0, 1.0 - (age.TotalSeconds / maxAge.TotalSeconds));

                // Frequency score (more accesses = higher score)
                var frequencyScore = Math.Min(1.0, temporal.accessCount / 10.0); // Cap at 10 accesses

                temporalScore = recencyWeight * recencyScore + frequencyWeight * frequencyScore;
                temporalContext = $"Last accessed {temporal.lastAccessed:yyyy-MM-dd}, accessed {temporal.accessCount} times";
            }

            var combinedScore = 0.5 * semanticScore + 0.5 * temporalScore; // 50/50 split
            results.Add((nodeId, combinedScore, temporalContext));
        }

        return results.OrderByDescending(x => x.score).ToList();
    }

    /// <summary>
    /// Generates a descriptive context string for a ranked result.
    /// </summary>
    private static string GenerateContextDescription(
        ulong nodeId,
        double semanticScore,
        double topologicalScore,
        double communityScore,
        ulong? nodeCommunity,
        ulong? queryCommunity,
        int degree)
    {
        var parts = new List<string>();

        // Semantic description
        if (semanticScore > 0.8)
            parts.Add("Highly semantically similar");
        else if (semanticScore > 0.6)
            parts.Add("Semantically related");
        else if (semanticScore > 0.4)
            parts.Add("Moderately similar");
        else
            parts.Add("Distantly related");

        // Topological description
        if (degree > 10)
            parts.Add("Highly connected");
        else if (degree > 5)
            parts.Add("Well connected");
        else if (degree > 2)
            parts.Add("Moderately connected");
        else
            parts.Add("Sparsely connected");

        // Community description
        if (queryCommunity.HasValue && nodeCommunity.HasValue)
        {
            if (nodeCommunity.Value == queryCommunity.Value)
                parts.Add("Same community");
            else
                parts.Add("Different community");
        }

        return string.Join(", ", parts);
    }
}
