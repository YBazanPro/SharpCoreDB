// <copyright file="JwtTokenService.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SharpCoreDB.Server.Core.Security;

/// <summary>
/// Produces and validates JWT bearer tokens for gRPC clients.
/// C# 14: Primary constructor with immutable fields.
/// </summary>
public sealed class JwtTokenService(
    string secretKey,
    int expirationHours = 24)
{
    private readonly string _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
    private readonly int _expirationHours = expirationHours;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Generates a new JWT token for the specified username and session.
    /// </summary>
    public string GenerateToken(string username, string sessionId, string? roles = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var key = Encoding.ASCII.GetBytes(_secretKey);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim("session_id", sessionId),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };

        if (!string.IsNullOrWhiteSpace(roles))
        {
            foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_expirationHours),
            SigningCredentials = signingCredentials,
            Audience = "sharpcoredb-server",
            Issuer = "sharpcoredb-server",
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates and extracts claims from a JWT token.
    /// </summary>
    public ClaimsPrincipal ValidateToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "sharpcoredb-server",
                ValidateAudience = true,
                ValidAudience = "sharpcoredb-server",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenInvalidSignatureException("Token signature validation failed");
            }

            return principal;
        }
        catch (Exception ex)
        {
            throw new SecurityTokenValidationException($"Token validation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts the session ID from a JWT token.
    /// </summary>
    public string? GetSessionIdFromToken(ClaimsPrincipal principal)
    {
        return principal?.FindFirst("session_id")?.Value;
    }

    /// <summary>
    /// Extracts the username from a JWT token.
    /// </summary>
    public string? GetUsernameFromToken(ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
