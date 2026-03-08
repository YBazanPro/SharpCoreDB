// <copyright file="CertificateAuthenticationTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharpCoreDB.Server.Core;
using SharpCoreDB.Server.Core.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for CertificateAuthenticationService — mTLS validation, thumbprint-to-role mapping,
/// and certificate identity extraction.
/// </summary>
public sealed class CertificateAuthenticationTests : IDisposable
{
    private readonly X509Certificate2 _testCert;

    public CertificateAuthenticationTests()
    {
        _testCert = CreateSelfSignedCert("CN=test-client");
    }

    public void Dispose()
    {
        _testCert.Dispose();
    }

    // ── ValidateAndMapCertificate ──

    [Fact]
    public void ValidateAndMapCertificate_ValidCert_ReturnsAuthenticated()
    {
        var service = CreateService(enableMtls: true);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.True(result.IsAuthenticated);
        Assert.NotNull(result.Principal);
        Assert.Equal(DatabaseRole.Reader, result.Role);
    }

    [Fact]
    public void ValidateAndMapCertificate_MtlsDisabled_ReturnsFailed()
    {
        var service = CreateService(enableMtls: false);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.False(result.IsAuthenticated);
        Assert.Contains("not enabled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAndMapCertificate_ExpiredCert_ReturnsFailed()
    {
        using var expiredCert = CreateSelfSignedCert("CN=expired-client",
            notBefore: DateTime.UtcNow.AddDays(-365), notAfter: DateTime.UtcNow.AddDays(-1));
        var service = CreateService(enableMtls: true);

        var result = service.ValidateAndMapCertificate(expiredCert);

        Assert.False(result.IsAuthenticated);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAndMapCertificate_FutureCert_ReturnsFailed()
    {
        using var futureCert = CreateSelfSignedCert("CN=future-client",
            notBefore: DateTime.UtcNow.AddDays(1), notAfter: DateTime.UtcNow.AddDays(365));
        var service = CreateService(enableMtls: true);

        var result = service.ValidateAndMapCertificate(futureCert);

        Assert.False(result.IsAuthenticated);
        Assert.Contains("not yet valid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ── Thumbprint-to-Role Mapping ──

    [Fact]
    public void ValidateAndMapCertificate_MappedThumbprint_ReturnsConfiguredRole()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping
            {
                Thumbprint = _testCert.Thumbprint,
                Role = "admin",
                Description = "Test admin cert",
            },
        ]);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.True(result.IsAuthenticated);
        Assert.Equal(DatabaseRole.Admin, result.Role);
    }

    [Fact]
    public void ValidateAndMapCertificate_WriterMapping_ReturnsWriter()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping
            {
                Thumbprint = _testCert.Thumbprint,
                Role = "writer",
            },
        ]);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.True(result.IsAuthenticated);
        Assert.Equal(DatabaseRole.Writer, result.Role);
    }

    [Fact]
    public void ValidateAndMapCertificate_UnmappedThumbprint_DefaultsToReader()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping
            {
                Thumbprint = "0000000000000000000000000000000000000000",
                Role = "admin",
            },
        ]);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.True(result.IsAuthenticated);
        Assert.Equal(DatabaseRole.Reader, result.Role);
    }

    // ── ResolveRole ──

    [Fact]
    public void ResolveRole_MatchingThumbprint_ReturnsMappedRole()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping { Thumbprint = "AABB", Role = "admin" },
        ]);

        Assert.Equal(DatabaseRole.Admin, service.ResolveRole("AABB"));
    }

    [Fact]
    public void ResolveRole_CaseInsensitive_Matches()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping { Thumbprint = "aabb", Role = "writer" },
        ]);

        Assert.Equal(DatabaseRole.Writer, service.ResolveRole("AABB"));
    }

    [Fact]
    public void ResolveRole_NoMapping_DefaultsToReader()
    {
        var service = CreateService(enableMtls: true);

        Assert.Equal(DatabaseRole.Reader, service.ResolveRole("NONEXISTENT"));
    }

    // ── Principal Construction ──

    [Fact]
    public void ValidateAndMapCertificate_PrincipalContainsExpectedClaims()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping { Thumbprint = _testCert.Thumbprint, Role = "admin" },
        ]);

        var result = service.ValidateAndMapCertificate(_testCert);

        Assert.True(result.IsAuthenticated);
        var principal = result.Principal!;

        // Should have a NameIdentifier (CN)
        var nameId = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(nameId);
        Assert.Equal("test-client", nameId.Value);

        // Should have auth method = X509
        var authMethod = principal.FindFirst(ClaimTypes.AuthenticationMethod);
        Assert.NotNull(authMethod);
        Assert.Equal("X509", authMethod.Value);

        // Should have thumbprint claim
        var thumbprint = principal.FindFirst("certificate_thumbprint");
        Assert.NotNull(thumbprint);
        Assert.Equal(_testCert.Thumbprint, thumbprint.Value);

        // Should have role claim
        var role = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(role);
        Assert.Equal("admin", role.Value);
    }

    [Fact]
    public void ValidateAndMapCertificate_RbacIntegration_PrincipalRoleResolvesCorrectly()
    {
        var service = CreateService(enableMtls: true, mappings:
        [
            new CertificateRoleMapping { Thumbprint = _testCert.Thumbprint, Role = "writer" },
        ]);

        var result = service.ValidateAndMapCertificate(_testCert);
        var resolvedRole = RbacService.GetRoleFromPrincipal(result.Principal!);

        Assert.Equal(DatabaseRole.Writer, resolvedRole);
    }

    // ── Helpers ──

    private static CertificateAuthenticationService CreateService(
        bool enableMtls,
        List<CertificateRoleMapping>? mappings = null)
    {
        var config = new ServerConfiguration
        {
            ServerName = "Test",
            BindAddress = "127.0.0.1",
            GrpcPort = 0,
            DefaultDatabase = "testdb",
            Databases = [new DatabaseInstanceConfiguration { Name = "testdb", DatabasePath = "test.db" }],
            Security = new SecurityConfiguration
            {
                TlsCertificatePath = "dummy.pem",
                TlsPrivateKeyPath = "dummy.key",
                JwtSecretKey = "integration-test-secret-key-32chars!!",
                EnableMutualTls = enableMtls,
                CertificateRoleMappings = mappings ?? [],
            },
        };

        return new CertificateAuthenticationService(
            Options.Create(config),
            NullLogger<CertificateAuthenticationService>.Instance);
    }

    private static X509Certificate2 CreateSelfSignedCert(
        string subjectName,
        DateTime? notBefore = null,
        DateTime? notAfter = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var effectiveNotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-5);
        var effectiveNotAfter = notAfter ?? DateTime.UtcNow.AddYears(1);

        return request.CreateSelfSigned(
            new DateTimeOffset(effectiveNotBefore),
            new DateTimeOffset(effectiveNotAfter));
    }
}
