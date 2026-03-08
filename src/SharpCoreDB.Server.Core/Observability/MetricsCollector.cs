// <copyright file="MetricsCollector.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace SharpCoreDB.Server.Core.Observability;

/// <summary>
/// Centralized metrics collection for SharpCoreDB server.
/// Tracks gRPC performance, database operations, and resource utilization.
/// C# 14: Primary constructor with immutable dependencies.
/// </summary>
public sealed class MetricsCollector : IDisposable
{
    private readonly Meter _meter;
    private readonly UpDownCounter<int> _activeConnections;
    private readonly Counter<long> _totalRequests;
    private readonly Histogram<double> _requestLatencyMs;
    private readonly UpDownCounter<long> _totalBytesReceived;
    private readonly UpDownCounter<long> _totalBytesSent;
    private readonly Counter<long> _failedRequests;
    private readonly UpDownCounter<int> _activeSessions;
    private readonly Histogram<int> _rowsReturned;

    public MetricsCollector(string serviceName = "sharpcoredb-server")
    {
        _meter = new Meter(serviceName, "1.5.0");

        _activeConnections = _meter.CreateUpDownCounter<int>(
            "sharpcoredb.connections.active",
            unit: "{connection}",
            description: "Current number of active gRPC connections");

        _totalRequests = _meter.CreateCounter<long>(
            "sharpcoredb.requests.total",
            unit: "{request}",
            description: "Total number of gRPC requests processed");

        _requestLatencyMs = _meter.CreateHistogram<double>(
            "sharpcoredb.request.latency_ms",
            unit: "ms",
            description: "gRPC request latency in milliseconds");

        _totalBytesReceived = _meter.CreateUpDownCounter<long>(
            "sharpcoredb.network.bytes_received",
            unit: "By",
            description: "Total bytes received from clients");

        _totalBytesSent = _meter.CreateUpDownCounter<long>(
            "sharpcoredb.network.bytes_sent",
            unit: "By",
            description: "Total bytes sent to clients");

        _failedRequests = _meter.CreateCounter<long>(
            "sharpcoredb.requests.failed",
            unit: "{request}",
            description: "Total number of failed gRPC requests");

        _activeSessions = _meter.CreateUpDownCounter<int>(
            "sharpcoredb.sessions.active",
            unit: "{session}",
            description: "Current number of active database sessions");

        _rowsReturned = _meter.CreateHistogram<int>(
            "sharpcoredb.query.rows_returned",
            unit: "{row}",
            description: "Number of rows returned per query");
    }

    /// <summary>
    /// Records a successful gRPC request with latency and payload metrics.
    /// </summary>
    public void RecordSuccessfulRequest(
        string method,
        double latencyMs,
        long bytesReceived,
        long bytesSent,
        int rowsReturned = 0)
    {
        var tags = new TagList { { "method", method } };

        _totalRequests.Add(1, tags);
        _requestLatencyMs.Record(latencyMs, tags);
        _totalBytesReceived.Add(bytesReceived, tags);
        _totalBytesSent.Add(bytesSent, tags);

        if (rowsReturned > 0)
        {
            _rowsReturned.Record(rowsReturned, tags);
        }
    }

    /// <summary>
    /// Records a failed gRPC request.
    /// </summary>
    public void RecordFailedRequest(string method, string? errorCode = null)
    {
        var tags = new TagList
        {
            { "method", method },
            { "error_code", errorCode ?? "unknown" }
        };

        _failedRequests.Add(1, tags);
    }

    /// <summary>
    /// Increments active connection count.
    /// </summary>
    public void IncrementActiveConnections()
    {
        _activeConnections.Add(1);
    }

    /// <summary>
    /// Decrements active connection count.
    /// </summary>
    public void DecrementActiveConnections()
    {
        _activeConnections.Add(-1);
    }

    /// <summary>
    /// Increments active session count.
    /// </summary>
    public void IncrementActiveSessions()
    {
        _activeSessions.Add(1);
    }

    /// <summary>
    /// Decrements active session count.
    /// </summary>
    public void DecrementActiveSessions()
    {
        _activeSessions.Add(-1);
    }

    /// <summary>
    /// Gets the underlying OpenTelemetry Meter for custom instrument creation.
    /// </summary>
    public Meter Meter => _meter;

    /// <inheritdoc />
    public void Dispose()
    {
        _meter?.Dispose();
    }
}
