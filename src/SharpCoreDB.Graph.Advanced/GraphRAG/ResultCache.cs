#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.GraphRAG;

/// <summary>
/// Caches community detection results to avoid recomputation.
/// Uses table name and algorithm type as cache key.
/// </summary>
public class ResultCache
{
    private readonly ConcurrentDictionary<string, CachedResult> _cache = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Cached result with metadata.
    /// </summary>
    private readonly record struct CachedResult(
        object? Result,
        DateTime Timestamp,
        TimeSpan Ttl
    );

    /// <summary>
    /// Gets or computes community detection results.
    /// </summary>
    /// <typeparam name="T">Type of community result.</typeparam>
    /// <param name="cacheKey">Unique key for this computation.</param>
    /// <param name="computeFunc">Function to compute the result if not cached.</param>
    /// <param name="ttl">Time to live for the cached result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or computed result.</returns>
    public async Task<T> GetOrComputeAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> computeFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(computeFunc);

        var effectiveTtl = ttl ?? _defaultTtl;

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (!IsExpired(cached))
            {
                return (T)cached.Result!;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(cacheKey, out _);
            }
        }

        // Compute new result
        var result = await computeFunc(cancellationToken);

        // Cache the result
        var cachedResult = new CachedResult(result, DateTime.UtcNow, effectiveTtl);
        _cache[cacheKey] = cachedResult;

        return result;
    }

    /// <summary>
    /// Gets cached community detection results if available.
    /// </summary>
    /// <param name="tableName">Name of the graph table.</param>
    /// <param name="algorithm">Algorithm used (e.g., "louvain", "lp", "connected").</param>
    /// <returns>Cached communities or null if not available.</returns>
    public List<(ulong nodeId, ulong communityId)>? GetCommunities(string tableName, string algorithm)
    {
        var cacheKey = BuildCommunityCacheKey(tableName, algorithm);

        if (_cache.TryGetValue(cacheKey, out var cached) && !IsExpired(cached))
        {
            return cached.Result as List<(ulong nodeId, ulong communityId)>;
        }

        return null;
    }

    /// <summary>
    /// Caches community detection results.
    /// </summary>
    /// <param name="tableName">Name of the graph table.</param>
    /// <param name="algorithm">Algorithm used.</param>
    /// <param name="communities">Community results to cache.</param>
    /// <param name="ttl">Time to live.</param>
    public void CacheCommunities(
        string tableName,
        string algorithm,
        List<(ulong nodeId, ulong communityId)> communities,
        TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentNullException.ThrowIfNull(communities);

        var cacheKey = BuildCommunityCacheKey(tableName, algorithm);
        var effectiveTtl = ttl ?? _defaultTtl;

        var cachedResult = new CachedResult(communities, DateTime.UtcNow, effectiveTtl);
        _cache[cacheKey] = cachedResult;
    }

    /// <summary>
    /// Gets cached graph metrics if available.
    /// </summary>
    /// <param name="tableName">Name of the graph table.</param>
    /// <param name="metricType">Type of metric (e.g., "degree", "betweenness").</param>
    /// <returns>Cached metrics or null if not available.</returns>
    public List<(ulong nodeId, double value)>? GetMetrics(string tableName, string metricType)
    {
        var cacheKey = BuildMetricCacheKey(tableName, metricType);

        if (_cache.TryGetValue(cacheKey, out var cached) && !IsExpired(cached))
        {
            return cached.Result as List<(ulong nodeId, double value)>;
        }

        return null;
    }

    /// <summary>
    /// Caches graph metrics results.
    /// </summary>
    /// <param name="tableName">Name of the graph table.</param>
    /// <param name="metricType">Type of metric.</param>
    /// <param name="metrics">Metric results to cache.</param>
    /// <param name="ttl">Time to live.</param>
    public void CacheMetrics(
        string tableName,
        string metricType,
        List<(ulong nodeId, double value)> metrics,
        TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricType);
        ArgumentNullException.ThrowIfNull(metrics);

        var cacheKey = BuildMetricCacheKey(tableName, metricType);
        var effectiveTtl = ttl ?? _defaultTtl;

        var cachedResult = new CachedResult(metrics, DateTime.UtcNow, effectiveTtl);
        _cache[cacheKey] = cachedResult;
    }

    /// <summary>
    /// Clears all cached results.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    /// <returns>Number of entries removed.</returns>
    public int CleanupExpired()
    {
        var expiredKeys = _cache
            .Where(kvp => IsExpired(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        return expiredKeys.Count;
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    public (int totalEntries, int expiredEntries, long memoryUsage) GetStatistics()
    {
        var totalEntries = _cache.Count;
        var expiredEntries = _cache.Count(kvp => IsExpired(kvp.Value));

        // Rough memory estimation (not accurate but useful for monitoring)
        var memoryUsage = _cache.Sum(kvp => EstimateMemoryUsage(kvp.Value.Result));

        return (totalEntries, expiredEntries, memoryUsage);
    }

    /// <summary>
    /// Builds a cache key for community results.
    /// </summary>
    private static string BuildCommunityCacheKey(string tableName, string algorithm)
    {
        return $"communities:{tableName}:{algorithm}";
    }

    /// <summary>
    /// Builds a cache key for metric results.
    /// </summary>
    private static string BuildMetricCacheKey(string tableName, string metricType)
    {
        return $"metrics:{tableName}:{metricType}";
    }

    /// <summary>
    /// Checks if a cached result has expired.
    /// </summary>
    private static bool IsExpired(CachedResult cached)
    {
        return DateTime.UtcNow - cached.Timestamp > cached.Ttl;
    }

    /// <summary>
    /// Estimates memory usage of a cached result (rough approximation).
    /// </summary>
    private static long EstimateMemoryUsage(object? result)
    {
        if (result is null)
        {
            return 0;
        }

        return result switch
        {
            List<(ulong, ulong)> communities => communities.Count * 16, // Rough estimate: 8 + 8 bytes per tuple
            List<(ulong, double)> metrics => metrics.Count * 16, // Rough estimate: 8 + 8 bytes per tuple
            _ => 1024 // Default estimate for unknown types
        };
    }
}

/// <summary>
/// Extension methods for ResultCache.
/// </summary>
public static class ResultCacheExtensions
{
    /// <summary>
    /// Gets or computes community detection results with caching.
    /// </summary>
    public static async Task<List<(ulong nodeId, ulong communityId)>> GetOrComputeCommunitiesAsync(
        this ResultCache cache,
        string tableName,
        string algorithm,
        Func<CancellationToken, Task<List<(ulong nodeId, ulong communityId)>>> computeFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        var cacheKey = $"communities:{tableName}:{algorithm}";
        return await cache.GetOrComputeAsync(cacheKey, computeFunc, ttl, cancellationToken);
    }

    /// <summary>
    /// Gets or computes graph metrics with caching.
    /// </summary>
    public static async Task<List<(ulong nodeId, double value)>> GetOrComputeMetricsAsync(
        this ResultCache cache,
        string tableName,
        string metricType,
        Func<CancellationToken, Task<List<(ulong nodeId, double value)>>> computeFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        var cacheKey = $"metrics:{tableName}:{metricType}";
        return await cache.GetOrComputeAsync(cacheKey, computeFunc, ttl, cancellationToken);
    }
}
