#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpCoreDB.Graph.Advanced.CommunityDetection;

namespace SharpCoreDB.Graph.Advanced.SqlIntegration;

/// <summary>
/// SQL function implementations for community detection.
/// These functions integrate with SharpCoreDB's SQL query engine.
/// </summary>
public static class CommunityDetectionFunctions
{
    /// <summary>
    /// SQL: DETECT_COMMUNITIES_LOUVAIN(graph_table_name)
    /// Detects communities using Louvain algorithm.
    /// Returns: (node_id, community_id)
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong communityId)>> DetectCommunitiesLouvainAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table exists and has required columns
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Louvain algorithm
        var algorithm = new LouvainAlgorithm();
        var result = await algorithm.ExecuteAsync(graphData, cancellationToken);

        // Return results as (node_id, community_id) pairs
        return result.Communities
            .SelectMany(community => community.Members.Select(member => (member, community.Id)))
            .OrderBy(x => x.member)
            .ToList();
    }

    /// <summary>
    /// SQL: DETECT_COMMUNITIES_LP(graph_table_name)
    /// Detects communities using Label Propagation algorithm.
    /// Returns: (node_id, community_id)
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong communityId)>> DetectCommunitiesLPAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Label Propagation algorithm
        var algorithm = new LabelPropagationAlgorithm();
        var result = await algorithm.ExecuteAsync(graphData, cancellationToken);

        // Return results
        return result.Communities
            .SelectMany(community => community.Members.Select(member => (member, community.Id)))
            .OrderBy(x => x.member)
            .ToList();
    }

    /// <summary>
    /// SQL: GET_CONNECTED_COMPONENTS(graph_table_name)
    /// Finds connected components (weakly connected if directed).
    /// Returns: (node_id, component_id)
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong componentId)>> GetConnectedComponentsAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Connected Components algorithm
        var algorithm = new ConnectedComponentsAlgorithm();
        var result = await algorithm.ExecuteAsync(graphData, cancellationToken);

        // Return results
        return result.Communities
            .SelectMany(community => community.Members.Select(member => (member, community.Id)))
            .OrderBy(x => x.member)
            .ToList();
    }

    /// <summary>
    /// SQL: COMMUNITY_MEMBERS(graph_name, community_id)
    /// Gets all nodes in a specific community.
    /// </summary>
    public static List<ulong> GetCommunityMembers(
        Database database,
        string tableName,
        ulong communityId,
        string sourceColumn = "source",
        string targetColumn = "target")
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // For this function, we need to run community detection first
        // In a real implementation, this would be cached or stored
        // For now, we'll use Louvain as the default
        var task = Task.Run(() => DetectCommunitiesLouvainAsync(database, tableName, sourceColumn, targetColumn));
        var communities = task.GetAwaiter().GetResult();

        return communities
            .Where(x => x.communityId == communityId)
            .Select(x => x.nodeId)
            .OrderBy(id => id)
            .ToList();
    }

    /// <summary>
    /// SQL: COMMUNITY_SIZE(graph_name, community_id)
    /// Gets the size of a community.
    /// </summary>
    public static int GetCommunitySize(
        Database database,
        string tableName,
        ulong communityId,
        string sourceColumn = "source",
        string targetColumn = "target")
    {
        var members = GetCommunityMembers(database, tableName, communityId, sourceColumn, targetColumn);
        return members.Count;
    }

    /// <summary>
    /// SQL: COMMUNITY_DENSITY(graph_name, community_id)
    /// Gets the density of a community (0 to 1).
    /// </summary>
    public static double GetCommunityDensity(
        Database database,
        string tableName,
        ulong communityId,
        string sourceColumn = "source",
        string targetColumn = "target")
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // For density calculation, we need to run the algorithm and find the specific community
        var task = Task.Run(() => DetectCommunitiesLouvainAsync(database, tableName, sourceColumn, targetColumn));
        var communities = task.GetAwaiter().GetResult();

        // Load graph to calculate density
        var graphTask = Task.Run(() => GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn));
        var graphData = graphTask.GetAwaiter().GetResult();

        // Find the community
        var communityMembers = communities
            .Where(x => x.communityId == communityId)
            .Select(x => x.nodeId)
            .ToList();

        if (communityMembers.Count == 0)
        {
            return 0.0;
        }

        // Calculate density: actual edges / max possible edges
        var memberSet = new HashSet<ulong>(communityMembers);
        int actualEdges = 0;

        // Count edges within the community
        foreach (var member in communityMembers)
        {
            var memberIndex = Array.IndexOf(graphData.NodeIds, member);
            if (memberIndex >= 0)
            {
                foreach (var neighborIndex in graphData.AdjacencyList[memberIndex])
                {
                    var neighborId = graphData.NodeIds[neighborIndex];
                    if (memberSet.Contains(neighborId) && member < neighborId) // Count each edge once
                    {
                        actualEdges++;
                    }
                }
            }
        }

        int n = communityMembers.Count;
        int maxEdges = n * (n - 1) / 2;

        return maxEdges > 0 ? (double)actualEdges / maxEdges : 0.0;
    }
}

/// <summary>
/// SQL function implementations for graph metrics.
/// </summary>
public static class GraphMetricsFunctions
{
    /// <summary>
    /// SQL: BETWEENNESS_CENTRALITY(graph_table_name)
    /// Calculates betweenness centrality for all nodes.
    /// Returns: (node_id, centrality_value)
    /// </summary>
    public static async Task<List<(ulong nodeId, double centrality)>> CalculateBetweennessCentralityAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Betweenness Centrality algorithm
        var metric = new SharpCoreDB.Graph.Advanced.Metrics.BetweennessCentrality();
        var results = await metric.CalculateAsync(graphData, cancellationToken);

        // Return results
        return results
            .Select(r => (r.NodeId, r.Value))
            .OrderBy(x => x.NodeId)
            .ToList();
    }

    /// <summary>
    /// SQL: CLOSENESS_CENTRALITY(graph_table_name)
    /// Calculates closeness centrality for all nodes.
    /// </summary>
    public static async Task<List<(ulong nodeId, double centrality)>> CalculateClosenessCentralityAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Closeness Centrality algorithm
        var metric = new SharpCoreDB.Graph.Advanced.Metrics.ClosenessCentrality();
        var results = await metric.CalculateAsync(graphData, cancellationToken);

        // Return results
        return results
            .Select(r => (r.NodeId, r.Value))
            .OrderBy(x => x.NodeId)
            .ToList();
    }

    /// <summary>
    /// SQL: EIGENVECTOR_CENTRALITY(graph_table_name)
    /// Calculates eigenvector centrality for all nodes.
    /// </summary>
    public static async Task<List<(ulong nodeId, double centrality)>> CalculateEigenvectorCentralityAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Eigenvector Centrality algorithm
        var metric = new SharpCoreDB.Graph.Advanced.Metrics.EigenvectorCentrality();
        var results = await metric.CalculateAsync(graphData, cancellationToken);

        // Return results
        return results
            .Select(r => (r.NodeId, r.Value))
            .OrderBy(x => x.NodeId)
            .ToList();
    }

    /// <summary>
    /// SQL: CLUSTERING_COEFFICIENT(graph_table_name)
    /// Calculates local clustering coefficient for all nodes.
    /// Returns: (node_id, coefficient)
    /// </summary>
    public static async Task<List<(ulong nodeId, double coefficient)>> CalculateClusteringCoefficientAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Clustering Coefficient algorithm
        var metric = new SharpCoreDB.Graph.Advanced.Metrics.ClusteringCoefficient();
        var results = await metric.CalculateAsync(graphData, cancellationToken);

        // Return results
        return results
            .Select(r => (r.NodeId, r.Value))
            .OrderBy(x => x.NodeId)
            .ToList();
    }

    /// <summary>
    /// SQL: GLOBAL_CLUSTERING_COEFFICIENT(graph_table_name)
    /// Calculates average clustering coefficient for the graph.
    /// </summary>
    public static async Task<double> CalculateGlobalClusteringCoefficientAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        var localCoefficients = await CalculateClusteringCoefficientAsync(database, tableName, sourceColumn, targetColumn, cancellationToken);
        return localCoefficients.Count > 0 ? localCoefficients.Average(x => x.coefficient) : 0.0;
    }

    /// <summary>
    /// SQL: DEGREE_CENTRALITY(graph_table_name)
    /// Calculates normalized degree for all nodes.
    /// </summary>
    public static async Task<List<(ulong nodeId, double degree)>> CalculateDegreeCentralityAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Execute Degree Centrality algorithm
        var metric = new SharpCoreDB.Graph.Advanced.Metrics.DegreeCentrality();
        var results = await metric.CalculateAsync(graphData, cancellationToken);

        // Return results
        return results
            .Select(r => (r.NodeId, r.Value))
            .OrderBy(x => x.NodeId)
            .ToList();
    }
}

/// <summary>
/// SQL function implementations for sub-graph queries.
/// </summary>
public static class SubgraphFunctions
{
    /// <summary>
    /// SQL: EXTRACT_SUBGRAPH(graph_table_name, root_node, depth)
    /// Extracts subgraph within specified distance from root.
    /// Returns: (node_id, edge_from, edge_to, distance_from_root)
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong edgeFrom, ulong edgeTo, int distance)>> 
        ExtractSubgraphAsync(
        Database database,
        string tableName,
        ulong rootNode,
        int maxDepth,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));

        // Validate table
        if (!GraphLoader.ValidateGraphTable(database, tableName, sourceColumn, targetColumn))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist or lacks required columns '{sourceColumn}', '{targetColumn}'");
        }

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, tableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        if (graphData.NodeCount == 0)
        {
            return [];
        }

        // Find root node index
        var rootIndex = Array.IndexOf(graphData.NodeIds, rootNode);
        if (rootIndex < 0)
        {
            return []; // Root node not found
        }

        // BFS to extract subgraph
        var visited = new bool[graphData.NodeCount];
        var distances = new int[graphData.NodeCount];
        var queue = new Queue<int>();
        var result = new List<(ulong, ulong, ulong, int)>();

        Array.Fill(distances, -1);
        distances[rootIndex] = 0;
        visited[rootIndex] = true;
        queue.Enqueue(rootIndex);

        while (queue.Count > 0 && distances[queue.Peek()] < maxDepth)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentIndex = queue.Dequeue();
            var currentId = graphData.NodeIds[currentIndex];
            var currentDistance = distances[currentIndex];

            // Add all edges from this node within depth
            foreach (var neighborIndex in graphData.AdjacencyList[currentIndex])
            {
                var neighborId = graphData.NodeIds[neighborIndex];

                // Add edge to result
                result.Add((neighborId, currentId, neighborId, currentDistance + 1));

                if (!visited[neighborIndex] && currentDistance + 1 <= maxDepth)
                {
                    visited[neighborIndex] = true;
                    distances[neighborIndex] = currentDistance + 1;
                    queue.Enqueue(neighborIndex);
                }
            }
        }

        return result.OrderBy(x => x.Item4).ThenBy(x => x.Item1).ToList();
    }

    /// <summary>
    /// SQL: COMMUNITY_SEMANTIC_CONTEXT(node_id, graph_table_name, max_distance)
    /// Gets semantic context from community and neighbors.
    /// Returns: (related_node_id, distance, semantic_distance, community_context)
    /// </summary>
    public static async Task<List<(ulong relatedNodeId, int distance, double semanticDistance, string context)>> 
        CommunitySematicContextAsync(
        Database database,
        ulong nodeId,
        string graphTableName,
        int maxDistance,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(graphTableName);
        if (maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance));

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, graphTableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        // Find node index
        var nodeIndex = Array.IndexOf(graphData.NodeIds, nodeId);
        if (nodeIndex < 0)
        {
            return []; // Node not found
        }

        // Get community information
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(
            database, graphTableName, sourceColumn, targetColumn, cancellationToken);

        var nodeCommunity = communities.FirstOrDefault(c => c.nodeId == nodeId).communityId;

        // BFS to find nodes within distance
        var visited = new bool[graphData.NodeCount];
        var distances = new int[graphData.NodeCount];
        var queue = new Queue<int>();

        Array.Fill(distances, -1);
        distances[nodeIndex] = 0;
        visited[nodeIndex] = true;
        queue.Enqueue(nodeIndex);

        var results = new List<(ulong, int, double, string)>();

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentIndex = queue.Dequeue();
            var currentId = graphData.NodeIds[currentIndex];
            var currentDistance = distances[currentIndex];

            if (currentDistance > maxDistance) break;

            // Add to results (skip the original node)
            if (currentId != nodeId)
            {
                var relatedCommunity = communities.FirstOrDefault(c => c.nodeId == currentId).communityId;
                var semanticDistance = currentDistance * 0.1; // Mock semantic distance
                var context = relatedCommunity == nodeCommunity 
                    ? $"Same community {nodeCommunity}" 
                    : $"Different community {relatedCommunity}";

                results.Add((currentId, currentDistance, semanticDistance, context));
            }

            // Explore neighbors
            if (currentDistance < maxDistance)
            {
                foreach (var neighborIndex in graphData.AdjacencyList[currentIndex])
                {
                    if (!visited[neighborIndex])
                    {
                        visited[neighborIndex] = true;
                        distances[neighborIndex] = currentDistance + 1;
                        queue.Enqueue(neighborIndex);
                    }
                }
            }
        }

        return results.OrderBy(x => x.Item2).ThenBy(x => x.Item1).ToList();
    }
}

/// <summary>
/// SQL function implementations for GraphRAG enhancement.
/// </summary>
public static class GraphRagFunctions
{
    /// <summary>
    /// SQL: SEMANTIC_SEARCH_WITH_COMMUNITY(query, graph_table_name)
    /// Performs semantic search with community context.
    /// Returns: (node_id, community_id, relevance_score, context_description)
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong communityId, double relevanceScore, string context)>> 
        SemanticSearchWithCommunityAsync(
        Database database,
        string query,
        string graphTableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(graphTableName);

        // For now, this is a placeholder implementation
        // In a full GraphRAG system, this would:
        // 1. Use vector search to find semantically similar nodes
        // 2. Detect communities in the graph
        // 3. Rank results by semantic similarity + community context
        // 4. Return enriched results with community information

        // Placeholder: Just detect communities and return basic results
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(
            database, graphTableName, sourceColumn, targetColumn, cancellationToken);

        // Mock semantic search results (in real implementation, this would use vector search)
        var mockResults = communities
            .Take(10) // Limit to top 10 for demo
            .Select((community, index) => 
            {
                var relevance = 1.0 - (index * 0.1); // Mock relevance score
                var context = $"Community {community.communityId} with {communities.Count(c => c.communityId == community.communityId)} members";
                return (community.nodeId, community.communityId, relevance, context);
            })
            .ToList();

        return mockResults;
    }

    /// <summary>
    /// SQL: COMMUNITY_SEMANTIC_CONTEXT(node_id, graph_table_name, max_distance)
    /// Gets semantic context from community and neighbors.
    /// Returns: (related_node_id, distance, semantic_distance, community_context)
    /// </summary>
    public static async Task<List<(ulong relatedNodeId, int distance, double semanticDistance, string context)>> 
        CommunitySematicContextAsync(
        Database database,
        ulong nodeId,
        string graphTableName,
        int maxDistance,
        string sourceColumn = "source",
        string targetColumn = "target",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(graphTableName);
        if (maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance));

        // Load graph data
        var graphData = await GraphLoader.LoadFromTableAsync(database, graphTableName, sourceColumn, targetColumn,
            cancellationToken: cancellationToken);

        // Find node index
        var nodeIndex = Array.IndexOf(graphData.NodeIds, nodeId);
        if (nodeIndex < 0)
        {
            return []; // Node not found
        }

        // Get community information
        var communities = await CommunityDetectionFunctions.DetectCommunitiesLouvainAsync(
            database, graphTableName, sourceColumn, targetColumn, cancellationToken);

        var nodeCommunity = communities.FirstOrDefault(c => c.nodeId == nodeId).communityId;

        // BFS to find nodes within distance
        var visited = new bool[graphData.NodeCount];
        var distances = new int[graphData.NodeCount];
        var queue = new Queue<int>();

        Array.Fill(distances, -1);
        distances[nodeIndex] = 0;
        visited[nodeIndex] = true;
        queue.Enqueue(nodeIndex);

        var results = new List<(ulong, int, double, string)>();

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentIndex = queue.Dequeue();
            var currentId = graphData.NodeIds[currentIndex];
            var currentDistance = distances[currentIndex];

            if (currentDistance > maxDistance) break;

            // Add to results (skip the original node)
            if (currentId != nodeId)
            {
                var relatedCommunity = communities.FirstOrDefault(c => c.nodeId == currentId).communityId;
                var semanticDistance = currentDistance * 0.1; // Mock semantic distance
                var context = relatedCommunity == nodeCommunity 
                    ? $"Same community {nodeCommunity}" 
                    : $"Different community {relatedCommunity}";

                results.Add((currentId, currentDistance, semanticDistance, context));
            }

            // Explore neighbors
            if (currentDistance < maxDistance)
            {
                foreach (var neighborIndex in graphData.AdjacencyList[currentIndex])
                {
                    if (!visited[neighborIndex])
                    {
                        visited[neighborIndex] = true;
                        distances[neighborIndex] = currentDistance + 1;
                        queue.Enqueue(neighborIndex);
                    }
                }
            }
        }

        return results.OrderBy(x => x.Item2).ThenBy(x => x.Item1).ToList();
    }
}
