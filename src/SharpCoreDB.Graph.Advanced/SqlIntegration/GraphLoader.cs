#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpCoreDB.Graph.Advanced.SqlIntegration;

/// <summary>
/// Utility for loading graph data from SharpCoreDB tables.
/// Supports various edge table schemas and converts to GraphData format.
/// </summary>
public static class GraphLoader
{
    /// <summary>
    /// Loads graph data from a database table containing edges.
    /// Supports multiple column schemas for flexibility.
    /// </summary>
    /// <param name="database">The SharpCoreDB database instance.</param>
    /// <param name="tableName">Name of the table containing edge data.</param>
    /// <param name="sourceColumn">Column name for source node (default: "source").</param>
    /// <param name="targetColumn">Column name for target node (default: "target").</param>
    /// <param name="weightColumn">Optional column name for edge weights (default: null).</param>
    /// <param name="directed">Whether the graph is directed (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GraphData ready for algorithm processing.</returns>
    /// <exception cref="ArgumentException">If required columns are missing.</exception>
    public static async Task<GraphData> LoadFromTableAsync(
        Database database,
        string tableName,
        string sourceColumn = "source",
        string targetColumn = "target",
        string? weightColumn = null,
        bool directed = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetColumn);

        // Query all edges from the table
        var query = $"SELECT {sourceColumn}, {targetColumn}{(weightColumn is not null ? $", {weightColumn}" : "")} FROM {tableName}";
        var rows = database.ExecuteQuery(query, []);

        if (rows.Count == 0)
        {
            // Return empty graph
            return new GraphData
            {
                NodeIds = [],
                AdjacencyList = [],
                IsDirected = directed
            };
        }

        // Collect all unique nodes
        var nodeSet = new HashSet<ulong>();
        var edges = new List<(ulong source, ulong target, double? weight)>();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!row.TryGetValue(sourceColumn, out var sourceValue) ||
                !row.TryGetValue(targetColumn, out var targetValue))
            {
                throw new ArgumentException($"Required columns '{sourceColumn}' or '{targetColumn}' not found in table '{tableName}'");
            }

            // Convert to ulong (assuming node IDs are stored as integers)
            var sourceId = ConvertToUlong(sourceValue);
            var targetId = ConvertToUlong(targetValue);

            nodeSet.Add(sourceId);
            nodeSet.Add(targetId);

            double? weight = null;
            if (weightColumn is not null && row.TryGetValue(weightColumn, out var weightValue))
            {
                weight = ConvertToDouble(weightValue);
            }

            edges.Add((sourceId, targetId, weight));
        }

        // Sort nodes for consistent indexing
        var nodeIds = nodeSet.OrderBy(id => id).ToArray();
        var nodeIndexMap = nodeIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        // Build adjacency list
        var adjacencyList = new List<int>[nodeIds.Length];
        var edgeWeights = weightColumn is not null ? new List<List<double>>() : null;

        for (int i = 0; i < nodeIds.Length; i++)
        {
            adjacencyList[i] = new List<int>();
            if (edgeWeights is not null)
            {
                edgeWeights.Add(new List<double>());
            }
        }

        foreach (var (sourceId, targetId, weight) in edges)
        {
            var sourceIndex = nodeIndexMap[sourceId];
            var targetIndex = nodeIndexMap[targetId];

            adjacencyList[sourceIndex].Add(targetIndex);

            if (!directed)
            {
                adjacencyList[targetIndex].Add(sourceIndex);
            }

            if (edgeWeights is not null && weight.HasValue)
            {
                edgeWeights![sourceIndex].Add(weight.Value);
                if (!directed)
                {
                    edgeWeights![targetIndex].Add(weight.Value);
                }
            }
        }

        // Convert to arrays
        var adjacencyArrays = adjacencyList.Select(list => list.ToArray()).ToArray();
        var weightArrays = edgeWeights?.Select(list => list.ToArray()).ToArray();

        return new GraphData
        {
            NodeIds = nodeIds,
            AdjacencyList = adjacencyArrays,
            EdgeWeights = weightArrays,
            IsDirected = directed
        };
    }

    /// <summary>
    /// Loads graph data from a table with ROWREF columns (for GraphRAG).
    /// Assumes edges are stored as ROWREF references.
    /// </summary>
    public static async Task<GraphData> LoadFromRowRefTableAsync(
        Database database,
        string tableName,
        string sourceRefColumn = "source_ref",
        string targetRefColumn = "target_ref",
        bool directed = false,
        CancellationToken cancellationToken = default)
    {
        // ROWREF is a special SharpCoreDB type for graph edges
        // For now, delegate to regular table loading
        return await LoadFromTableAsync(database, tableName, sourceRefColumn, targetRefColumn,
            directed: directed, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Validates that a table exists and has the required columns for graph loading.
    /// </summary>
    public static bool ValidateGraphTable(Database database, string tableName,
        string sourceColumn = "source", string targetColumn = "target", string? weightColumn = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        try
        {
            // Check if table exists by trying to query it
            var testQuery = $"SELECT COUNT(*) FROM {tableName}";
            database.ExecuteQuery(testQuery, null);

            // Check if required columns exist
            var columnQuery = $"SELECT {sourceColumn}, {targetColumn} FROM {tableName} LIMIT 1";
            if (weightColumn is not null)
            {
                columnQuery = $"SELECT {sourceColumn}, {targetColumn}, {weightColumn} FROM {tableName} LIMIT 1";
            }

            database.ExecuteQuery(columnQuery, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts various numeric types to ulong.
    /// </summary>
    private static ulong ConvertToUlong(object value)
    {
        return value switch
        {
            ulong u => u,
            long l => (ulong)l,
            int i => (ulong)i,
            uint ui => ui,
            short s => (ulong)s,
            ushort us => us,
            byte b => b,
            sbyte sb => (ulong)sb,
            string str => ulong.Parse(str),
            _ => throw new ArgumentException($"Cannot convert {value.GetType()} to ulong")
        };
    }

    /// <summary>
    /// Converts various numeric types to double.
    /// </summary>
    private static double ConvertToDouble(object value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            uint ui => ui,
            short s => s,
            ushort us => us,
            byte b => b,
            sbyte sb => sb,
            string str => double.Parse(str),
            _ => throw new ArgumentException($"Cannot convert {value.GetType()} to double")
        };
    }
}
