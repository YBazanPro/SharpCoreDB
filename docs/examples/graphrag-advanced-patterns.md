# GraphRAG Advanced Patterns Tutorial

**SharpCoreDB.Graph.Advanced v2.0.0**  
**Time to Complete:** 30 minutes  
**Difficulty:** Advanced  
**Prerequisites:** Basic GraphRAG tutorial completed

## Overview

This tutorial covers advanced GraphRAG patterns including multi-hop reasoning, temporal ranking, custom ranking algorithms, and production deployment strategies.

## Advanced Ranking Patterns

### Multi-Hop Reasoning

Multi-hop reasoning considers indirect connections through the graph, providing richer context for search results.

```csharp
// Standard search (direct connections only)
var directResults = await engine.SearchAsync(queryEmbedding, topK: 5, maxHops: 0);

// Multi-hop search (up to 3 degrees of separation)
var multiHopResults = await engine.SearchAsync(queryEmbedding, topK: 5, maxHops: 3);

// Compare results
Console.WriteLine("Direct vs Multi-Hop Results:");
for (int i = 0; i < Math.Min(directResults.Count, multiHopResults.Count); i++)
{
    var direct = directResults[i];
    var multiHop = multiHopResults[i];
    Console.WriteLine($"Position {i + 1}:");
    Console.WriteLine($"  Direct: Node {direct.NodeId} (Score: {direct.CombinedScore:F3})");
    Console.WriteLine($"  Multi-Hop: Node {multiHop.NodeId} (Score: {multiHop.CombinedScore:F3})");
}
```

**When to use multi-hop reasoning:**
- Knowledge graphs with complex relationships
- Social networks where indirect connections matter
- Recommendation systems needing broader context

### Custom Ranking Weights

Different applications require different ranking priorities:

```csharp
// Knowledge graph - prioritize semantic similarity
var knowledgeGraphWeights = (semantic: 0.7, topological: 0.2, community: 0.1);

// Social network - prioritize community connections
var socialNetworkWeights = (semantic: 0.3, topological: 0.3, community: 0.4);

// Research network - prioritize topological importance
var researchNetworkWeights = (semantic: 0.4, topological: 0.5, community: 0.1);

// Apply custom weights
var results = await engine.SearchAsync(
    queryEmbedding,
    topK: 10,
    rankingWeights: knowledgeGraphWeights);
```

### Temporal Ranking

Incorporate recency and access frequency for time-aware results:

```csharp
// Create temporal data (in production, this comes from your usage tracking)
var temporalData = new Dictionary<ulong, (DateTime lastAccessed, int accessCount)>
{
    [1] = (DateTime.UtcNow.AddDays(-1), 15),    // Recently accessed frequently
    [2] = (DateTime.UtcNow.AddDays(-30), 3),    // Old, rarely accessed
    [3] = (DateTime.UtcNow.AddHours(-1), 8),    // Very recent, moderately accessed
    [4] = (DateTime.UtcNow.AddDays(-7), 25),    // Week old, very frequently accessed
    [5] = (DateTime.UtcNow.AddDays(-90), 1)     // Very old, rarely accessed
};

// Apply temporal ranking
var temporalResults = EnhancedRanking.RankWithTemporal(
    semanticResults,
    temporalData,
    recencyWeight: 0.4,    // 40% weight on how recent
    frequencyWeight: 0.3); // 30% weight on access frequency

Console.WriteLine("Temporal Ranking Results:");
foreach (var (nodeId, score, context) in temporalResults)
{
    Console.WriteLine($"Node {nodeId}: Score {score:F3} - {context}");
}
```

## Custom Ranking Algorithms

### Implementing Domain-Specific Ranking

```csharp
// Custom ranking algorithm for research collaboration
public class ResearchCollaborationRanker
{
    public List<EnhancedRanking.RankedResult> Rank(
        List<(ulong nodeId, double semanticScore)> semanticResults,
        GraphData graphData,
        List<(ulong nodeId, ulong communityId)> communities,
        Dictionary<ulong, ResearcherProfile> researcherProfiles)
    {
        var results = new List<EnhancedRanking.RankedResult>();

        foreach (var (nodeId, semanticScore) in semanticResults)
        {
            var nodeIndex = Array.IndexOf(graphData.NodeIds, nodeId);
            if (nodeIndex < 0) continue;

            // Get research-specific metrics
            var profile = researcherProfiles[nodeId];
            var publications = profile.PublicationCount;
            var citations = profile.TotalCitations;
            var collaborationScore = profile.CollaborationScore;

            // Custom scoring formula for research
            var researchScore = semanticScore * 0.4 +
                              (publications / 100.0) * 0.3 +  // Normalize publications
                              (citations / 1000.0) * 0.2 +   // Normalize citations
                              collaborationScore * 0.1;

            // Community bonus for same research field
            var communityBonus = communities.Any(c => c.nodeId == nodeId &&
                researcherProfiles.ContainsKey(c.nodeId) &&
                researcherProfiles[c.nodeId].Field == profile.Field) ? 0.1 : 0.0;

            var finalScore = Math.Min(researchScore + communityBonus, 1.0);

            var context = GenerateResearchContext(nodeId, profile, publications, citations);

            results.Add(new EnhancedRanking.RankedResult(
                NodeId: nodeId,
                SemanticScore: semanticScore,
                TopologicalScore: collaborationScore,
                CommunityScore: communityBonus,
                CombinedScore: finalScore,
                Context: context
            ));
        }

        return results.OrderByDescending(r => r.CombinedScore).ToList();
    }

    private string GenerateResearchContext(ulong nodeId, ResearcherProfile profile,
        int publications, int citations)
    {
        return $"Researcher {nodeId}: {profile.Field}, {publications} publications, " +
               $"{citations} citations, {profile.Institution}";
    }
}

// Usage
var ranker = new ResearchCollaborationRanker();
var customResults = ranker.Rank(semanticResults, graphData, communities, researcherProfiles);
```

### Researcher Profile Structure

```csharp
public record ResearcherProfile(
    string Name,
    string Field,           // "Computer Science", "Physics", etc.
    string Institution,
    int PublicationCount,
    int TotalCitations,
    double CollaborationScore,  // 0.0 to 1.0 based on co-authorship network
    DateTime LastPublication,
    List<string> Keywords
);
```

## Production Deployment Patterns

### High-Throughput Configuration

```csharp
// Configure for high-throughput GraphRAG
var services = new ServiceCollection();
services.AddSharpCoreDB()
    .AddVectorSupport(options =>
    {
        options.EnableQueryOptimization = true;
        options.DefaultIndexType = VectorIndexType.Hnsw;
        options.MaxCacheSize = 10_000_000;  // 10M vectors
        options.EnableMemoryPooling = true;
        options.HnswEfConstruction = 300;   // Higher quality index
        options.HnswM = 32;                 // More connections per node
    });

var engine = new GraphRagEngine(database, "production_graph", "production_embeddings", 1536);

// Pre-warm caches
await WarmupCaches(engine);
```

### Cache Warming Strategy

```csharp
private static async Task WarmupCaches(GraphRagEngine engine)
{
    // Warm up community detection cache
    var communityTasks = new[]
    {
        engine.GetCache().GetOrComputeCommunitiesAsync("graph1", "louvain", ComputeCommunities),
        engine.GetCache().GetOrComputeCommunitiesAsync("graph2", "louvain", ComputeCommunities),
        // Add more graphs as needed
    };

    await Task.WhenAll(communityTasks);

    // Warm up metric caches
    var metricTasks = new[]
    {
        engine.GetCache().GetOrComputeMetricsAsync("graph1", "degree_centrality", ComputeDegrees),
        engine.GetCache().GetOrComputeMetricsAsync("graph1", "betweenness_centrality", ComputeBetweenness),
    };

    await Task.WhenAll(metricTasks);
}
```

### Memory Management for Large Graphs

```csharp
// Process large graphs in batches
await MemoryOptimizer.ProcessInBatchesAsync(
    largeNodeSet,
    batchSize: 1000,
    async (batch, ct) =>
    {
        // Process batch of nodes
        var batchEmbeddings = await GenerateEmbeddingsForBatch(batch);
        await engine.IndexEmbeddingsAsync(batchEmbeddings);

        // Force GC between batches for large datasets
        GC.Collect();
        await Task.Yield();
    });

// Monitor memory usage
var (totalEntries, expiredEntries, memoryUsage) = engine.GetCacheStatistics();
if (memoryUsage > 500 * 1024 * 1024) // 500MB
{
    Console.WriteLine("High memory usage detected, consider cache cleanup");
    var removed = engine.GetCache().CleanupExpired();
    Console.WriteLine($"Cleaned up {removed} expired cache entries");
}
```

## Integration with Embedding Providers

### OpenAI Integration

```csharp
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiEmbeddingProvider(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _apiKey = apiKey;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, int dimensions = 1536)
    {
        var request = new
        {
            input = text,
            model = dimensions == 1536 ? "text-embedding-3-large" : "text-embedding-3-small",
            dimensions = dimensions
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings", request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>();
        return result!.Data[0].Embedding;
    }

    public async Task<Dictionary<ulong, float[]>> GenerateEmbeddingsBatchAsync(
        Dictionary<ulong, string> nodeTexts, int dimensions = 1536)
    {
        // Batch requests to avoid rate limits
        var results = new Dictionary<ulong, float[]>();
        var batchSize = 100; // OpenAI limit

        foreach (var batch in nodeTexts.Chunk(batchSize))
        {
            var batchTexts = batch.Select(kvp => kvp.Value).ToArray();
            var embeddings = await GenerateEmbeddingsAsync(batchTexts, dimensions);

            for (int i = 0; i < batch.Length; i++)
            {
                results[batch[i].Key] = embeddings[i];
            }

            // Rate limiting
            await Task.Delay(1000); // 1 request per second
        }

        return results;
    }

    private async Task<float[][]> GenerateEmbeddingsAsync(string[] texts, int dimensions)
    {
        var request = new
        {
            input = texts,
            model = dimensions == 1536 ? "text-embedding-3-large" : "text-embedding-3-small",
            dimensions = dimensions
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings", request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>();
        return result!.Data.Select(d => d.Embedding).ToArray();
    }
}

public record OpenAiEmbeddingResponse(
    List<EmbeddingData> Data
);

public record EmbeddingData(
    float[] Embedding
);
```

### Usage with GraphRAG

```csharp
// Initialize with OpenAI embeddings
var embeddingProvider = new OpenAiEmbeddingProvider(apiKey);
var engine = new GraphRagEngine(database, "knowledge_graph", "embeddings", 1536);
await engine.InitializeAsync();

// Generate embeddings for all nodes
var nodeTexts = await GetNodeTextsFromDatabase(database);
var embeddings = await embeddingProvider.GenerateEmbeddingsBatchAsync(nodeTexts, 1536);

// Convert to NodeEmbedding format
var nodeEmbeddings = embeddings.Select(kvp =>
    new VectorSearchIntegration.NodeEmbedding(kvp.Key, kvp.Value)).ToList();

// Index in GraphRAG
await engine.IndexEmbeddingsAsync(nodeEmbeddings);

// Now perform searches with real embeddings
var queryText = "machine learning algorithms for graph analysis";
var queryEmbedding = await embeddingProvider.GenerateEmbeddingAsync(queryText, 1536);
var results = await engine.SearchAsync(queryEmbedding, topK: 10);
```

## Performance Monitoring and Tuning

### Comprehensive Benchmarking

```csharp
// Run full performance benchmark
var queryEmbedding = await embeddingProvider.GenerateEmbeddingAsync("test query", 1536);
var benchmarks = await PerformanceProfiler.RunComprehensiveBenchmarkAsync(engine, queryEmbedding);

// Generate detailed report
var report = PerformanceProfiler.GeneratePerformanceReport(benchmarks);
Console.WriteLine(report);

// Example output:
// === GraphRAG Performance Report ===
// Generated: 2026-03-30 14:30:22 UTC
//
// Operation: GraphRAG Search (k=10)
//   Duration: 45.23ms
//   Memory Used: 2.34 KB
//   Nodes Processed: 10
//   Edges Processed: 0
//   Operations/sec: 221.02
//
// Operation: Vector Search (k=10)
//   Duration: 12.45ms
//   Memory Used: 1.12 KB
//   Nodes Processed: 10
//   Edges Processed: 0
//   Operations/sec: 803.21
//
// === Summary Statistics ===
// Average Operation Time: 28.45ms
// Total Memory Used: 3.46 KB
// Total Nodes Processed: 20
//
// === Recommendations ===
// - Current performance is excellent
// - Consider caching for repeated queries
```

### Identifying Bottlenecks

```csharp
// Profile individual components
var searchMetrics = await PerformanceProfiler.ProfileSearchAsync(engine, queryEmbedding);
var vectorMetrics = await PerformanceProfiler.ProfileVectorSearchAsync(
    database, "embeddings", queryEmbedding);
var communityMetrics = await PerformanceProfiler.ProfileCommunityDetectionAsync(
    database, "graph", "louvain");

// Compare and identify bottlenecks
if (vectorMetrics.TotalDuration > searchMetrics.TotalDuration * 0.5)
{
    Console.WriteLine("Vector search is the bottleneck - consider HNSW optimization");
}

if (communityMetrics.TotalDuration > TimeSpan.FromSeconds(1))
{
    Console.WriteLine("Community detection is slow - consider caching or approximation");
}
```

## Real-World Application Patterns

### Knowledge Graph Search

```csharp
public class KnowledgeGraphSearch
{
    private readonly GraphRagEngine _engine;
    private readonly IEmbeddingProvider _embeddings;

    public KnowledgeGraphSearch(GraphRagEngine engine, IEmbeddingProvider embeddings)
    {
        _engine = engine;
        _embeddings = embeddings;
    }

    public async Task<List<KnowledgeResult>> SearchKnowledgeAsync(
        string query, KnowledgeSearchOptions options)
    {
        // Generate query embedding
        var queryEmbedding = await _embeddings.GenerateEmbeddingAsync(query, options.EmbeddingDimensions);

        // Perform GraphRAG search
        var results = await _engine.SearchAsync(
            queryEmbedding,
            topK: options.MaxResults,
            includeCommunities: true,
            maxHops: options.MaxHops,
            rankingWeights: (semantic: 0.6, topological: 0.2, community: 0.2));

        // Enrich with knowledge-specific context
        var knowledgeResults = new List<KnowledgeResult>();
        foreach (var result in results)
        {
            var context = await _engine.GetNodeContextAsync(result.NodeId, maxDistance: 2);
            var knowledge = await GetKnowledgeDetailsAsync(result.NodeId);

            knowledgeResults.Add(new KnowledgeResult
            {
                NodeId = result.NodeId,
                Title = knowledge.Title,
                Content = knowledge.Content,
                RelevanceScore = result.CombinedScore,
                Context = result.Context,
                RelatedConcepts = context.GraphNeighbors.Select(id => GetConceptName(id)).ToList(),
                Community = context.CommunityId
            });
        }

        return knowledgeResults.OrderByDescending(r => r.RelevanceScore).ToList();
    }
}
```

### Social Network Recommendations

```csharp
public class SocialRecommendationEngine
{
    private readonly GraphRagEngine _engine;
    private readonly IEmbeddingProvider _embeddings;

    public async Task<List<UserRecommendation>> RecommendConnectionsAsync(
        ulong userId, int maxRecommendations = 10)
    {
        // Get user's profile embedding
        var userProfile = await GetUserProfileAsync(userId);
        var profileEmbedding = await _embeddings.GenerateEmbeddingAsync(
            userProfile.Interests + " " + userProfile.Bio, 768);

        // Search for similar users with community focus
        var candidates = await _engine.SearchAsync(
            profileEmbedding,
            topK: maxRecommendations * 2, // Get more for filtering
            includeCommunities: true,
            maxHops: 2, // Consider friend-of-friend connections
            rankingWeights: (semantic: 0.3, topological: 0.3, community: 0.4));

        // Filter out existing connections and self
        var existingConnections = await GetUserConnectionsAsync(userId);
        var recommendations = candidates
            .Where(c => c.NodeId != userId && !existingConnections.Contains(c.NodeId))
            .Take(maxRecommendations)
            .Select(async c =>
            {
                var profile = await GetUserProfileAsync(c.NodeId);
                return new UserRecommendation
                {
                    UserId = c.NodeId,
                    Name = profile.Name,
                    CommonInterests = CalculateCommonInterests(userProfile.Interests, profile.Interests),
                    MutualConnections = await GetMutualConnectionsCountAsync(userId, c.NodeId),
                    RelevanceScore = c.CombinedScore,
                    Reason = GenerateRecommendationReason(c)
                };
            });

        return (await Task.WhenAll(recommendations)).ToList();
    }

    private string GenerateRecommendationReason(EnhancedRanking.RankedResult result)
    {
        var reasons = new List<string>();

        if (result.SemanticScore > 0.7) reasons.Add("similar interests");
        if (result.TopologicalScore > 0.5) reasons.Add("well connected in network");
        if (result.CommunityScore > 0.8) reasons.Add("part of your community");

        return string.Join(", ", reasons);
    }
}
```

## Scaling Strategies

### Database Partitioning

```csharp
// Partition large graphs across multiple tables
public class PartitionedGraphRagEngine
{
    private readonly Dictionary<string, GraphRagEngine> _partitionEngines;

    public async Task<List<EnhancedRanking.RankedResult>> SearchAcrossPartitionsAsync(
        float[] queryEmbedding, int topK)
    {
        // Search all partitions in parallel
        var partitionTasks = _partitionEngines.Values
            .Select(engine => engine.SearchAsync(queryEmbedding, topK * 2))
            .ToArray();

        var partitionResults = await Task.WhenAll(partitionTasks);

        // Merge and re-rank results
        var allResults = partitionResults
            .SelectMany(results => results)
            .OrderByDescending(r => r.CombinedScore)
            .Take(topK)
            .ToList();

        return allResults;
    }
}
```

### Caching Strategies

```csharp
// Multi-level caching
public class MultiLevelCache
{
    private readonly ResultCache _memoryCache;
    private readonly IDistributedCache _distributedCache;

    public async Task<T> GetOrComputeAsync<T>(
        string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        // Try memory cache first
        var memoryKey = $"memory:{key}";
        // ... memory cache logic ...

        // Try distributed cache
        var distributedKey = $"distributed:{key}";
        var cached = await _distributedCache.GetStringAsync(distributedKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<T>(cached);
        }

        // Compute and cache
        var result = await factory();
        await _distributedCache.SetStringAsync(
            distributedKey,
            JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });

        return result;
    }
}
```

## Monitoring and Observability

### Structured Logging

```csharp
public class GraphRagLogger
{
    private readonly ILogger _logger;

    public void LogSearchPerformance(
        string operation, TimeSpan duration, long memoryUsed, int resultsCount)
    {
        _logger.LogInformation(
            "GraphRAG operation completed: {Operation}, Duration: {Duration}ms, " +
            "Memory: {Memory}KB, Results: {ResultsCount}",
            operation,
            duration.TotalMilliseconds,
            memoryUsed / 1024,
            resultsCount);
    }

    public void LogCacheStatistics(int totalEntries, int expiredEntries, long memoryUsage)
    {
        _logger.LogInformation(
            "Cache statistics: Total={Total}, Expired={Expired}, Memory={Memory}KB",
            totalEntries, expiredEntries, memoryUsage / 1024);

        if (expiredEntries > totalEntries * 0.1) // More than 10% expired
        {
            _logger.LogWarning("High cache expiration rate detected");
        }
    }
}
```

### Metrics Collection

```csharp
public class GraphRagMetrics
{
    private readonly IMeter _meter;

    public GraphRagMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("GraphRAG");
    }

    public void RecordSearchDuration(TimeSpan duration, string operation)
    {
        var histogram = _meter.CreateHistogram<double>(
            "graphrag_search_duration",
            "ms",
            "Duration of GraphRAG search operations");

        histogram.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object>("operation", operation));
    }

    public void RecordCacheHitRate(double hitRate)
    {
        var gauge = _meter.CreateObservableGauge(
            "graphrag_cache_hit_rate",
            () => hitRate,
            "%",
            "Cache hit rate for GraphRAG operations");
    }
}
```

## Conclusion

Advanced GraphRAG patterns provide powerful capabilities for complex search and recommendation scenarios. Key takeaways:

1. **Multi-hop reasoning** reveals indirect relationships
2. **Custom ranking weights** adapt to domain requirements
3. **Temporal ranking** incorporates recency and frequency
4. **Production deployment** requires careful caching and monitoring
5. **Integration with embedding providers** enables real semantic search
6. **Performance profiling** identifies and resolves bottlenecks
7. **Scaling strategies** handle large graphs and high throughput

These patterns enable sophisticated applications in knowledge graphs, social networks, recommendation systems, and research collaboration platforms.

---

**For basic usage, see:** `docs/examples/graphrag-basic-usage.md`  
**For API reference, see:** `docs/api/SharpCoreDB.Graph.Advanced.API.md`  
**For performance tuning, see:** `docs/performance/graphrag-performance-tuning.md`
