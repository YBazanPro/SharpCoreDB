// <copyright file="GrpcRequestMetricsInterceptor.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SharpCoreDB.Server.Core.Grpc;

/// <summary>
/// Interceptor for gRPC request telemetry and structured logging.
/// gRPC is the flagship protocol; this interceptor provides first-class observability.
/// </summary>
public sealed class GrpcRequestMetricsInterceptor(
    ILogger<GrpcRequestMetricsInterceptor> logger) : Interceptor
{
    private readonly ILogger<GrpcRequestMetricsInterceptor> _logger = logger;

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var response = await continuation(request, context);
            var elapsed = Stopwatch.GetElapsedTime(start);

            _logger.LogInformation(
                "gRPC unary completed: {Method} | {StatusCode} | {ElapsedMs:F2}ms",
                context.Method,
                context.Status.StatusCode,
                elapsed.TotalMilliseconds);

            return response;
        }
        catch (RpcException ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            _logger.LogWarning(
                ex,
                "gRPC unary failed: {Method} | {StatusCode} | {ElapsedMs:F2}ms",
                context.Method,
                ex.StatusCode,
                elapsed.TotalMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await continuation(request, responseStream, context);
            var elapsed = Stopwatch.GetElapsedTime(start);

            _logger.LogInformation(
                "gRPC server-streaming completed: {Method} | {StatusCode} | {ElapsedMs:F2}ms",
                context.Method,
                context.Status.StatusCode,
                elapsed.TotalMilliseconds);
        }
        catch (RpcException ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            _logger.LogWarning(
                ex,
                "gRPC server-streaming failed: {Method} | {StatusCode} | {ElapsedMs:F2}ms",
                context.Method,
                ex.StatusCode,
                elapsed.TotalMilliseconds);
            throw;
        }
    }
}
