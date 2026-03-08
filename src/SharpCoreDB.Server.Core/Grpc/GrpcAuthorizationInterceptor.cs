// <copyright file="GrpcAuthorizationInterceptor.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SharpCoreDB.Server.Core.Security;

namespace SharpCoreDB.Server.Core.Grpc;

/// <summary>
/// gRPC interceptor for JWT token validation, certificate auth, and RBAC authorization.
/// Supports both Bearer JWT tokens and client certificates (mutual TLS).
/// C# 14: Primary constructor with immutable dependencies.
/// </summary>
public sealed class GrpcAuthorizationInterceptor(
    JwtTokenService tokenService,
    RbacService rbacService,
    CertificateAuthenticationService certAuthService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GrpcAuthorizationInterceptor> logger) : Interceptor
{
    private readonly JwtTokenService _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    private readonly RbacService _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
    private readonly CertificateAuthenticationService _certAuthService = certAuthService ?? throw new ArgumentNullException(nameof(certAuthService));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly ILogger<GrpcAuthorizationInterceptor> _logger = logger;

    private static readonly HashSet<string> PublicMethods =
    [
        "sharpcoredb.DatabaseService/HealthCheck",
        "sharpcoredb.DatabaseService/Connect",
    ];

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ValidateToken(context);
        return await continuation(request, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateToken(context);
        await continuation(request, responseStream, context).ConfigureAwait(false);
    }

    private void ValidateToken(ServerCallContext context)
    {
        // Allow public methods without token
        if (IsPublicMethod(context.Method))
        {
            return;
        }

        // Extract bearer token from metadata
        var authHeader = context.RequestHeaders.FirstOrDefault(h =>
            h.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase));

        // Try JWT bearer token first
        if (authHeader is not null && !string.IsNullOrWhiteSpace(authHeader.Value))
        {
            ValidateJwtToken(authHeader.Value, context);
            return;
        }

        // Fall back to client certificate (mutual TLS)
        if (TryValidateClientCertificate(context))
        {
            return;
        }

        _logger.LogWarning("gRPC request to {Method} has no valid credentials (JWT or client certificate)", context.Method);
        throw new RpcException(new Status(StatusCode.Unauthenticated, "Authorization required (JWT bearer token or client certificate)"));
    }

    private void ValidateJwtToken(string authHeaderValue, ServerCallContext context)
    {
        var token = ExtractBearerToken(authHeaderValue);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("gRPC request to {Method} has invalid authorization header format", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token format"));
        }

        try
        {
            var principal = _tokenService.ValidateToken(token);
            var username = _tokenService.GetUsernameFromToken(principal);
            var sessionId = _tokenService.GetSessionIdFromToken(principal);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidOperationException("Token missing required claims");
            }

            // Enforce RBAC permissions
            if (!_rbacService.AuthorizeGrpcCall(principal, context.Method))
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied,
                    $"Insufficient permissions for {context.Method}"));
            }

            _logger.LogDebug("gRPC {Method} authorized for {Username} via JWT", context.Method, username);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "gRPC token validation failed for {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Token validation failed"));
        }
    }

    private bool TryValidateClientCertificate(ServerCallContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var clientCert = httpContext?.Connection.ClientCertificate;
        if (clientCert is null)
        {
            return false;
        }

        var result = _certAuthService.ValidateAndMapCertificate(clientCert);
        if (!result.IsAuthenticated || result.Principal is null)
        {
            _logger.LogWarning("gRPC client certificate rejected for {Method}: {Error}",
                context.Method, result.ErrorMessage);
            return false;
        }

        // Enforce RBAC permissions
        if (!_rbacService.AuthorizeGrpcCall(result.Principal, context.Method))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                $"Insufficient permissions for {context.Method}"));
        }

        _logger.LogDebug("gRPC {Method} authorized via client certificate", context.Method);
        return true;
    }

    private static bool IsPublicMethod(string method)
    {
        return PublicMethods.Any(pm => method.EndsWith(pm, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractBearerToken(string authHeaderValue)
    {
        const string bearerScheme = "Bearer ";
        if (authHeaderValue.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            return authHeaderValue[bearerScheme.Length..].Trim();
        }

        return null;
    }
}
