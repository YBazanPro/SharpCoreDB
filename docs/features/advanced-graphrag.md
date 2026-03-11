# Phase 12: Advanced GraphRAG - Community Detection & Graph Analytics

**Version:** 1.0.0  
**Status:** 🚀 IN PROGRESS (Started March 9, 2026)  
**Target Release:** April 13, 2026  

---

## 📋 Overview

Phase 12 extends SharpCoreDB.Graph with enterprise-grade community detection algorithms, advanced graph metrics, and sophisticated graph analytics for discovering patterns, clusters, and relationships in graph data.

### Key Features

#### 🔧 Community Detection
- **Louvain Algorithm** - Multi-level modularity optimization, O(n log n)
- **Label Propagation** - Distributed community detection, O(d*m)
- **Connected Components** - Union-Find with path compression, O(n*α(n))

**Use Cases:**
- Finding tightly connected groups in social networks
- Protein interaction networks
- Organization structure analysis
- Market segmentation

#### 📊 Graph Metrics
- **Betweenness Centrality** - Measure node "bridging" importance
- **Closeness Centrality** - How central a node is to others
- **Eigenvector Centrality** - Influence based on influential neighbors
- **Clustering Coefficient** - Local and global network clustering
- **Degree Centrality** - Direct connectivity baseline

**Use Cases:**
- Identifying influencers in networks
- Finding network bottlenecks
- Importance ranking
- Network robustness analysis

#### 🎯 Sub-graph Queries
- **K-Core Decomposition** - Finding dense subgraph cores
- **Clique Detection** - Finding complete subgraphs (Bron-Kerbosch)
- **Triangle Detection** - Finding 3-cliques

**Use Cases:**
- Community structure analysis
- Dense region identification
- Cluster validation
- Core-periphery structure

#### 🔗 GraphRAG Enhancement
- **Semantic Search with Community Context** - Search queries with graph topology awareness
- **Community-Aware Semantic Context** - Retrieve related nodes with community information

**Use Cases:**
- Knowledge graph search with semantic + topological ranking
- Question answering with context awareness
- Entity disambiguation
- Knowledge discovery

---

## 🚀 Quick Start

### Installation

```bash
dotnet add package SharpCoreDB.Graph.Advanced
```

### Basic Community Detection

```csharp
using SharpCoreDB.Graph.Advanced;
using SharpCoreDB.Graph.Advanced.CommunityDetection;

// Create or load your graph
var graph = new GraphData
{
    NodeIds = [1, 2, 3, 4, 5, 6],
    AdjacencyList = new[]
    {
        new[] { 1, 2 },        // Node 0 connected to 1, 2
        new[] { 0, 2 },        // Node 1 connected to 0, 2
        new[] { 0, 1 },        // Node 2 connected to 0, 1
        new[] { 4, 5 },        // Node 3 connected to 4, 5
        new[] { 3, 5 },        // Node 4 connected to 3, 5
        new[] { 3, 4 }         // Node 5 connected to 3, 4
    },
    IsDirected = false
};

// Detect communities using Louvain
var louvain = new LouvainAlgorithm();
var communities = await louvain.ExecuteAsync(graph);

foreach (var community in communities.Communities)
{
    Console.WriteLine($"Community {community.Id}: {community.Members.Count} members, density={community.Density:F3}");
    foreach (var nodeId in community.Members)
    {
        Console.WriteLine($"  - Node {nodeId}");
    }
}

// Access performance metrics
var metrics = louvain.LastExecutionMetrics;
Console.WriteLine($"Execution time: {metrics.Duration.TotalMilliseconds:F2}ms");
Console.WriteLine($"Modularity: {communities.OverallModularity:F4}");
```

### Calculate Graph Metrics

```csharp
using SharpCoreDB.Graph.Advanced.Metrics;

// Calculate degree centrality
var degreeCentrality = new DegreeCentrality();
var degreeResults = await degreeCentrality.CalculateAsync(graph);

foreach (var result in degreeResults.OrderByDescending(r => r.Value).Take(5))
{
    Console.WriteLine($"Node {result.NodeId}: degree = {result.Value:F3}");
}

// Calculate betweenness centrality
var betweenness = new BetweennessCentrality();
var betweennessResults = await betweenness.CalculateAsync(graph);

var topBridges = betweennessResults
    .OrderByDescending(r => r.Value)
    .Take(3)
    .ToList();

Console.WriteLine("Top bridge nodes:");
foreach (var result in topBridges)
{
    Console.WriteLine($"  Node {result.NodeId}: betweenness = {result.Value:F4}");
}

// Calculate clustering coefficient
var clustering = new ClusteringCoefficient();
var clusteringResults = await clustering.CalculateAsync(graph);

double globalClustering = clusteringResults.Average(r => r.Value);
Console.WriteLine($"Global clustering coefficient: {globalClustering:F4}");
```

### Find Sub-graphs

```csharp
using SharpCoreDB.Graph.Advanced.SubgraphQueries;

// Find triangles
var triangles = await TriangleDetector.DetectTrianglesAsync(graph);
Console.WriteLine($"Found {triangles.Count} triangles");

// Find cliques (minimum size 3)
var cliques = await CliqueDetector.FindMaximalCliquesAsync(graph, minCliqueSize: 3);
Console.WriteLine($"Found {cliques.Count} cliques");

foreach (var clique in cliques.OrderByDescending(c => c.Count).Take(5))
{
    Console.WriteLine($"  Clique: {string.Join(", ", clique)}");
}

// K-core decomposition
var (kCore, coreGroups) = await KCoreDecomposition.DecomposeAsync(graph);

for (int k = 1; k <= kCore.Max(); k++)
{
    var nodes = KCoreDecomposition.ExtractKCore(kCore, k);
    Console.WriteLine($"K-core {k}: {nodes.Count} nodes");
}
```

---

## 📊 Performance Characteristics

### Community Detection

| Algorithm | Time Complexity | Space | Modularity | Use Case |
|-----------|-----------------|-------|-----------|----------|
| Louvain | O(n log n) | O(n+m) | Excellent | General purpose |
| Label Propagation | O(d*m) | O(n) | Good | Large graphs |
| Connected Components | O(n*α(n)) | O(n) | N/A | Weakly connected |

### Graph Metrics

| Metric | Time | Space | Applications |
|--------|------|-------|--------------|
| Degree Centrality | O(n) | O(n) | Quick baseline |
| Closeness | O(n(n+m)) | O(n+m) | Global position |
| Betweenness | O(n(n+m)) | O(n+m) | Bridge detection |
| Eigenvector | O(k(n+m)) | O(n) | Influence ranking |
| Clustering | O(n*d²) | O(n) | Network cohesion |

### Sub-graph Detection

| Algorithm | Time | Applications |
|-----------|------|--------------|
| K-Core | O(n+m) | Dense regions |
| Triangles | O(m^1.5) | Clustering analysis |
| Cliques (Bron-Kerbosch) | O(3^(n/3)) | Community cores |

---

## 🎯 Common Use Cases

### 1. Social Network Analysis

```csharp
// Detect communities in social network
var communities = await new LouvainAlgorithm().ExecuteAsync(socialGraph);

// Find influencers (high eigenvector centrality)
var influencers = await new EigenvectorCentrality().CalculateAsync(socialGraph);

// Identify bridge users (high betweenness)
var bridges = await new BetweennessCentrality().CalculateAsync(socialGraph);
```

### 2. Protein Interaction Networks

```csharp
// Find protein complexes (communities)
var proteinComplexes = await new LouvainAlgorithm().ExecuteAsync(interactionGraph);

// Identify hub proteins (high degree/betweenness)
var hubs = await new DegreeCentrality().CalculateAsync(interactionGraph);

// Find tightly connected regions
var kCores = await KCoreDecomposition.DecomposeAsync(interactionGraph);
```

### 3. Knowledge Graph Search

```csharp
// Semantic search with community context
var results = await GraphRagFunctions.SemanticSearchWithCommunityAsync(
    query: "What are the key concepts related to machine learning?",
    graphTableName: "knowledge_graph"
);

// Get related entities with context
var context = await GraphRagFunctions.CommunitySematicContextAsync(
    nodeId: entityId,
    graphTableName: "knowledge_graph",
    maxDistance: 3
);
```

### 4. Organization Network Analysis

```csharp
// Detect teams/departments
var departments = await new LouvainAlgorithm().ExecuteAsync(orgGraph);

// Find cross-functional bridges
var bridges = await new BetweennessCentrality().CalculateAsync(orgGraph);

// Measure team cohesion
var clustering = await new ClusteringCoefficient().CalculateAsync(orgGraph);
```

---

## 📖 Advanced Topics

### Custom Graph Data Creation

```csharp
// Build from edge list
var edges = new List<(int from, int to)>
{
    (0, 1), (1, 2), (2, 0), // Triangle
    (3, 4), (4, 5), (5, 3)  // Another triangle
};

int nodeCount = 6;
var adjacencyList = new List<int>[nodeCount];
for (int i = 0; i < nodeCount; i++)
    adjacencyList[i] = new List<int>();

foreach (var (from, to) in edges)
{
    adjacencyList[from].Add(to);
    if (!directed)
        adjacencyList[to].Add(from);
}

var graph = new GraphData
{
    NodeIds = Enumerable.Range(0, nodeCount).Select(i => (ulong)i).ToArray(),
    AdjacencyList = adjacencyList.Select(l => l.ToArray()).ToArray(),
    IsDirected = false
};
```

### Weighted Graphs

```csharp
var graph = new GraphData
{
    NodeIds = [1, 2, 3],
    AdjacencyList = [[1, 2], [0, 2], [0, 1]],
    EdgeWeights = new[]
    {
        new[] { 0.0, 0.5, 0.3 },  // Weights from node 0
        new[] { 0.5, 0.0, 0.8 },  // Weights from node 1
        new[] { 0.3, 0.8, 0.0 }   // Weights from node 2
    },
    IsDirected = false
};
```

### Cancellation and Timeout

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    var result = await algorithm.ExecuteAsync(graph, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Algorithm timed out");
}
```

---

## 🔗 Integration with SharpCoreDB

### SQL Integration (Coming Soon)

```sql
-- Detect communities
SELECT * FROM DETECT_COMMUNITIES_LOUVAIN('my_graph_edges');

-- Calculate metrics
SELECT node_id, betweenness FROM BETWEENNESS_CENTRALITY('my_graph_edges');

-- Find cliques
SELECT * FROM FIND_CLIQUES('my_graph_edges', 3) LIMIT 10;

-- Semantic search with community context
SELECT * FROM SEMANTIC_SEARCH_WITH_COMMUNITY('query text', 'knowledge_graph');
```

---

## 📚 References

1. **Louvain Algorithm:** Blondel et al., "Fast unfolding of communities in large networks" (2008)
2. **Label Propagation:** Raghavan et al., "Near linear time algorithm to detect community structures" (2007)
3. **Betweenness Centrality:** Brandes, "A faster algorithm for betweenness centrality" (2001)
4. **K-Core:** Batagelj & Zaversnik, "An O(m) Algorithm for Cores Decomposition" (2003)
5. **Clique Detection:** Bron & Kerbosch, "Algorithm 457: Finding All Cliques of an Undirected Graph" (1973)

---

## 🐛 Troubleshooting

### Out of Memory

For large graphs (millions of nodes), use:
- Label Propagation (lower memory than Louvain)
- Degree Centrality (minimal memory)
- Streaming algorithms (process in chunks)

### Slow Performance

- Use Connected Components (fastest, O(n*α(n)))
- Sample the graph if needed
- Consider degree-based filtering first
- Use appropriate algorithm for your graph size

### Numerical Issues

- Eigenvector centrality may need more iterations for convergence
- Check `LastExecutionMetrics.CustomMetrics["converged"]`
- Ensure graph has no isolated nodes

---

**Next:** See `docs/api/graphrag-advanced-api.md` for detailed API reference.

**Examples:** See `Examples/GraphRAG/` for complete working examples.
