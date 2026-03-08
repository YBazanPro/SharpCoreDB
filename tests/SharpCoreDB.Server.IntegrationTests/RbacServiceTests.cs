// <copyright file="RbacServiceTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpCoreDB.Server.Core;
using SharpCoreDB.Server.Core.Security;
using System.Security.Claims;

namespace SharpCoreDB.Server.IntegrationTests;

/// <summary>
/// Tests for RBAC service — role mapping, permission enforcement, and authorization logic.
/// </summary>
public sealed class RbacServiceTests
{
    private readonly RbacService _rbac = new(NullLogger<RbacService>.Instance);

    // ── GetPermissionsForRole ──

    [Fact]
    public void GetPermissionsForRole_Reader_ReturnsReadPermissions()
    {
        var perms = RbacService.GetPermissionsForRole(DatabaseRole.Reader);

        Assert.True(perms.HasFlag(Permission.Read));
        Assert.True(perms.HasFlag(Permission.SchemaRead));
        Assert.True(perms.HasFlag(Permission.VectorSearch));
        Assert.True(perms.HasFlag(Permission.ViewMetrics));
        Assert.False(perms.HasFlag(Permission.Write));
        Assert.False(perms.HasFlag(Permission.SchemaModify));
    }

    [Fact]
    public void GetPermissionsForRole_Writer_IncludesReadAndWrite()
    {
        var perms = RbacService.GetPermissionsForRole(DatabaseRole.Writer);

        Assert.True(perms.HasFlag(Permission.Read));
        Assert.True(perms.HasFlag(Permission.Write));
        Assert.True(perms.HasFlag(Permission.Transaction));
        Assert.True(perms.HasFlag(Permission.Batch));
        Assert.False(perms.HasFlag(Permission.SchemaModify));
        Assert.False(perms.HasFlag(Permission.ManageUsers));
    }

    [Fact]
    public void GetPermissionsForRole_Admin_IncludesAllPermissions()
    {
        var perms = RbacService.GetPermissionsForRole(DatabaseRole.Admin);

        Assert.True(perms.HasFlag(Permission.Read));
        Assert.True(perms.HasFlag(Permission.Write));
        Assert.True(perms.HasFlag(Permission.SchemaModify));
        Assert.True(perms.HasFlag(Permission.CreateDatabase));
        Assert.True(perms.HasFlag(Permission.ManageUsers));
    }

    // ── HasPermission ──

    [Fact]
    public void HasPermission_ReaderCanRead()
    {
        Assert.True(RbacService.HasPermission(DatabaseRole.Reader, Permission.Read));
    }

    [Fact]
    public void HasPermission_ReaderCannotWrite()
    {
        Assert.False(RbacService.HasPermission(DatabaseRole.Reader, Permission.Write));
    }

    [Fact]
    public void HasPermission_WriterCanWrite()
    {
        Assert.True(RbacService.HasPermission(DatabaseRole.Writer, Permission.Write));
    }

    [Fact]
    public void HasPermission_WriterCannotModifySchema()
    {
        Assert.False(RbacService.HasPermission(DatabaseRole.Writer, Permission.SchemaModify));
    }

    [Fact]
    public void HasPermission_AdminCanManageUsers()
    {
        Assert.True(RbacService.HasPermission(DatabaseRole.Admin, Permission.ManageUsers));
    }

    // ── GetRoleFromPrincipal ──

    [Fact]
    public void GetRoleFromPrincipal_NoRoleClaims_DefaultsToReader()
    {
        var principal = CreatePrincipal("user1");

        var role = RbacService.GetRoleFromPrincipal(principal);

        Assert.Equal(DatabaseRole.Reader, role);
    }

    [Fact]
    public void GetRoleFromPrincipal_WithAdminRole_ReturnsAdmin()
    {
        var principal = CreatePrincipal("admin1", "admin");

        var role = RbacService.GetRoleFromPrincipal(principal);

        Assert.Equal(DatabaseRole.Admin, role);
    }

    [Fact]
    public void GetRoleFromPrincipal_WithWriterRole_ReturnsWriter()
    {
        var principal = CreatePrincipal("writer1", "writer");

        var role = RbacService.GetRoleFromPrincipal(principal);

        Assert.Equal(DatabaseRole.Writer, role);
    }

    [Fact]
    public void GetRoleFromPrincipal_WithMultipleRoles_ReturnsHighest()
    {
        var principal = CreatePrincipal("user1", "reader", "writer", "admin");

        var role = RbacService.GetRoleFromPrincipal(principal);

        Assert.Equal(DatabaseRole.Admin, role);
    }

    [Fact]
    public void GetRoleFromPrincipal_CaseInsensitive()
    {
        var principal = CreatePrincipal("user1", "Admin");

        var role = RbacService.GetRoleFromPrincipal(principal);

        Assert.Equal(DatabaseRole.Admin, role);
    }

    // ── AuthorizeGrpcCall ──

    [Fact]
    public void AuthorizeGrpcCall_ReaderCanExecuteQuery()
    {
        var principal = CreatePrincipal("reader1", "reader");

        Assert.True(_rbac.AuthorizeGrpcCall(principal, "sharpcoredb.DatabaseService/ExecuteQuery"));
    }

    [Fact]
    public void AuthorizeGrpcCall_ReaderCannotExecuteNonQuery()
    {
        var principal = CreatePrincipal("reader1", "reader");

        Assert.False(_rbac.AuthorizeGrpcCall(principal, "sharpcoredb.DatabaseService/ExecuteNonQuery"));
    }

    [Fact]
    public void AuthorizeGrpcCall_WriterCanExecuteNonQuery()
    {
        var principal = CreatePrincipal("writer1", "writer");

        Assert.True(_rbac.AuthorizeGrpcCall(principal, "sharpcoredb.DatabaseService/ExecuteNonQuery"));
    }

    [Fact]
    public void AuthorizeGrpcCall_WriterCanBeginTransaction()
    {
        var principal = CreatePrincipal("writer1", "writer");

        Assert.True(_rbac.AuthorizeGrpcCall(principal, "sharpcoredb.DatabaseService/BeginTransaction"));
    }

    [Fact]
    public void AuthorizeGrpcCall_UnknownMethodReturnsTrue()
    {
        // Unknown methods are not in the permission map — treated as public
        var principal = CreatePrincipal("anyone");

        Assert.True(_rbac.AuthorizeGrpcCall(principal, "sharpcoredb.DatabaseService/SomeNewMethod"));
    }

    // ── AuthorizeRestRequest ──

    [Fact]
    public void AuthorizeRestRequest_ReaderCanQuery()
    {
        var principal = CreatePrincipal("reader1", "reader");

        Assert.True(_rbac.AuthorizeRestRequest(principal, "POST", "/api/v1/query"));
    }

    [Fact]
    public void AuthorizeRestRequest_ReaderCannotExecute()
    {
        var principal = CreatePrincipal("reader1", "reader");

        Assert.False(_rbac.AuthorizeRestRequest(principal, "POST", "/api/v1/execute"));
    }

    [Fact]
    public void AuthorizeRestRequest_WriterCanBatch()
    {
        var principal = CreatePrincipal("writer1", "writer");

        Assert.True(_rbac.AuthorizeRestRequest(principal, "POST", "/api/v1/batch"));
    }

    [Fact]
    public void AuthorizeRestRequest_ReaderCanViewMetrics()
    {
        var principal = CreatePrincipal("reader1", "reader");

        Assert.True(_rbac.AuthorizeRestRequest(principal, "GET", "/api/v1/metrics"));
    }

    // ── Helpers ──

    private static ClaimsPrincipal CreatePrincipal(string username, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, username),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
