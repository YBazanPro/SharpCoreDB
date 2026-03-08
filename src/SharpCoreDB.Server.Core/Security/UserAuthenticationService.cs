// <copyright file="UserAuthenticationService.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace SharpCoreDB.Server.Core.Security;

/// <summary>
/// Authenticates users against the configured user store and issues JWT tokens.
/// Passwords are validated using SHA-256 hash comparison.
/// C# 14: Primary constructor, pattern matching, switch expressions.
/// </summary>
public sealed class UserAuthenticationService(
    IOptions<ServerConfiguration> configuration,
    JwtTokenService tokenService,
    ILogger<UserAuthenticationService> logger)
{
    private readonly ServerConfiguration _config = configuration.Value;
    private readonly JwtTokenService _tokenService = tokenService;
    private readonly ILogger<UserAuthenticationService> _logger = logger;

    /// <summary>
    /// Authenticates a user and returns a result with JWT token and role.
    /// </summary>
    /// <param name="username">Login username.</param>
    /// <param name="password">Plaintext password.</param>
    /// <param name="sessionId">Session identifier to embed in the token.</param>
    /// <returns>Authentication result.</returns>
    public AuthenticationResult Authenticate(string username, string password, string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var user = _config.Security.Users
            .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            _logger.LogWarning("Authentication failed: user '{Username}' not found", username);
            return AuthenticationResult.Failed("Invalid username or password");
        }

        var passwordHash = ComputeSha256Hash(password);
        if (!string.Equals(passwordHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Authentication failed: invalid password for user '{Username}'", username);
            return AuthenticationResult.Failed("Invalid username or password");
        }

        var role = ParseRole(user.Role);
        var token = _tokenService.GenerateToken(username, sessionId, user.Role);

        _logger.LogInformation(
            "User '{Username}' authenticated successfully with role '{Role}'",
            username, role);

        return AuthenticationResult.Succeeded(token, role);
    }

    /// <summary>
    /// Validates that a user exists and returns their configured role.
    /// Does not verify password — use for session lookups where JWT is already validated.
    /// </summary>
    /// <param name="username">Login username.</param>
    /// <returns>The user's role, or null if user not found.</returns>
    public DatabaseRole? GetUserRole(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = _config.Security.Users
            .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        return user is null ? null : ParseRole(user.Role);
    }

    private static DatabaseRole ParseRole(string role) => role.ToLowerInvariant() switch
    {
        "admin" => DatabaseRole.Admin,
        "writer" => DatabaseRole.Writer,
        "reader" => DatabaseRole.Reader,
        _ => DatabaseRole.Reader,
    };

    private static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }
}

/// <summary>
/// Result of a user authentication attempt.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>Whether authentication succeeded.</summary>
    public bool IsAuthenticated { get; private init; }

    /// <summary>JWT token (only set on success).</summary>
    public string? Token { get; private init; }

    /// <summary>Authenticated user's role (only set on success).</summary>
    public DatabaseRole Role { get; private init; }

    /// <summary>Error message (only set on failure).</summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>Creates a successful authentication result.</summary>
    public static AuthenticationResult Succeeded(string token, DatabaseRole role) => new()
    {
        IsAuthenticated = true,
        Token = token,
        Role = role,
    };

    /// <summary>Creates a failed authentication result.</summary>
    public static AuthenticationResult Failed(string errorMessage) => new()
    {
        IsAuthenticated = false,
        ErrorMessage = errorMessage,
    };
}
