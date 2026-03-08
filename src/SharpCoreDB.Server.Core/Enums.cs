// <copyright file="Enums.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Server.Core;

/// <summary>
/// Health status for monitoring.
/// </summary>
public enum HealthStatus
{
    /// <summary>Service is healthy.</summary>
    Healthy,

    /// <summary>Service is degraded but functional.</summary>
    Degraded,

    /// <summary>Service is unhealthy.</summary>
    Unhealthy
}

/// <summary>
/// Database access roles. A user may hold one role per database.
/// Higher roles include all lower-level permissions.
/// </summary>
public enum DatabaseRole
{
    /// <summary>Read-only access — SELECT queries only.</summary>
    Reader = 0,

    /// <summary>Read/write access — SELECT, INSERT, UPDATE, DELETE.</summary>
    Writer = 1,

    /// <summary>Full access — DDL, user management, server configuration.</summary>
    Admin = 2,
}

/// <summary>
/// Fine-grained permission flags for RBAC enforcement.
/// </summary>
[Flags]
public enum Permission
{
    /// <summary>No permissions.</summary>
    None = 0,

    /// <summary>Execute SELECT queries.</summary>
    Read = 1 << 0,

    /// <summary>Execute INSERT, UPDATE, DELETE statements.</summary>
    Write = 1 << 1,

    /// <summary>Execute DDL statements (CREATE, ALTER, DROP).</summary>
    SchemaModify = 1 << 2,

    /// <summary>Begin, commit, and rollback transactions.</summary>
    Transaction = 1 << 3,

    /// <summary>Execute batch statements.</summary>
    Batch = 1 << 4,

    /// <summary>Execute vector search operations.</summary>
    VectorSearch = 1 << 5,

    /// <summary>View database schema metadata.</summary>
    SchemaRead = 1 << 6,

    /// <summary>Create and drop databases.</summary>
    CreateDatabase = 1 << 7,

    /// <summary>Manage users and roles.</summary>
    ManageUsers = 1 << 8,

    /// <summary>View server metrics and health.</summary>
    ViewMetrics = 1 << 9,

    /// <summary>All reader permissions.</summary>
    AllReader = Read | SchemaRead | VectorSearch | ViewMetrics,

    /// <summary>All writer permissions (includes reader).</summary>
    AllWriter = AllReader | Write | Transaction | Batch,

    /// <summary>All admin permissions (includes writer).</summary>
    AllAdmin = AllWriter | SchemaModify | CreateDatabase | ManageUsers,
}
