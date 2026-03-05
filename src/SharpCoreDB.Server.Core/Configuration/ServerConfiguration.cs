// <copyright file="ServerConfiguration.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.Server.Core;

/// <summary>
/// Configuration options for SharpCoreDB network server.
/// Supports multi-database hosting and secure TLS-only endpoints.
/// </summary>
public sealed class ServerConfiguration
{
    /// <summary>Server name (displayed in monitoring).</summary>
    public required string ServerName { get; init; }

    /// <summary>Bind address (0.0.0.0 for all interfaces).</summary>
    public required string BindAddress { get; init; } = "0.0.0.0";

    /// <summary>gRPC HTTPS port (default: 5001).</summary>
    public required int GrpcPort { get; init; } = 5001;

    /// <summary>Enable gRPC protocol (HTTPS only).</summary>
    public bool EnableGrpc { get; init; } = true;

    /// <summary>Enable HTTPS API endpoint for diagnostics and management.</summary>
    public bool EnableHttpsApi { get; init; } = true;

    /// <summary>HTTPS API port (default: 8443).</summary>
    public int HttpsApiPort { get; init; } = 8443;

    /// <summary>Maximum concurrent connections.</summary>
    public int MaxConnections { get; init; } = 1000;

    /// <summary>Connection timeout (seconds).</summary>
    public int ConnectionTimeoutSeconds { get; init; } = 300;

    /// <summary>Default database name for clients that do not specify one.</summary>
    public required string DefaultDatabase { get; init; }

    /// <summary>Configured databases hosted by this server.</summary>
    public required List<DatabaseInstanceConfiguration> Databases { get; init; } = [];

    /// <summary>System database settings (master/model/msdb/tempdb style).</summary>
    public SystemDatabasesConfiguration SystemDatabases { get; init; } = new();

    /// <summary>Security configuration.</summary>
    public required SecurityConfiguration Security { get; init; }

    /// <summary>Logging configuration.</summary>
    public LoggingConfiguration Logging { get; init; } = new();

    /// <summary>Performance tuning.</summary>
    public PerformanceConfiguration Performance { get; init; } = new();
}

/// <summary>
/// Per-database hosted instance configuration.
/// </summary>
public sealed class DatabaseInstanceConfiguration
{
    /// <summary>Logical database name used by clients.</summary>
    public required string Name { get; init; }

    /// <summary>Physical database file path.</summary>
    public required string DatabasePath { get; init; }

    /// <summary>Storage mode (SingleFile, Directory, Columnar).</summary>
    public string StorageMode { get; init; } = "SingleFile";

    /// <summary>Enable encryption for this database.</summary>
    public bool EncryptionEnabled { get; init; }

    /// <summary>Encryption key file path for this database.</summary>
    public string? EncryptionKeyFile { get; init; }

    /// <summary>Connection pool size for this database.</summary>
    public int ConnectionPoolSize { get; init; } = 50;

    /// <summary>Marks this as a system database.</summary>
    public bool IsSystemDatabase { get; init; }

    /// <summary>Marks database as read-only.</summary>
    public bool IsReadOnly { get; init; }
}

/// <summary>
/// Configuration for optional system databases.
/// </summary>
public sealed class SystemDatabasesConfiguration
{
    /// <summary>Enable system databases.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Name of master system database.</summary>
    public string MasterDatabaseName { get; init; } = "master";

    /// <summary>Name of model system database.</summary>
    public string ModelDatabaseName { get; init; } = "model";

    /// <summary>Name of job/agent metadata system database.</summary>
    public string MsdbDatabaseName { get; init; } = "msdb";

    /// <summary>Name of temporary objects system database.</summary>
    public string TempDbDatabaseName { get; init; } = "tempdb";
}

/// <summary>
/// Security configuration.
/// </summary>
public sealed class SecurityConfiguration
{
    /// <summary>Require authentication.</summary>
    public bool RequireAuthentication { get; init; } = true;

    /// <summary>Authentication methods (jwt, apikey, certificate).</summary>
    public List<string> AuthMethods { get; init; } = ["jwt"];

    /// <summary>JWT secret (base64 encoded).</summary>
    public string? JwtSecret { get; init; }

    /// <summary>JWT expiration (minutes).</summary>
    public int JwtExpirationMinutes { get; init; } = 60;

    /// <summary>Enable TLS/SSL. Must be true in production.</summary>
    public bool TlsEnabled { get; init; } = true;

    /// <summary>TLS certificate file.</summary>
    public string? TlsCertificatePath { get; init; }

    /// <summary>TLS private key file.</summary>
    public string? TlsPrivateKeyPath { get; init; }

    /// <summary>Require client certificate (mTLS).</summary>
    public bool RequireClientCertificate { get; init; }

    /// <summary>Minimum TLS version policy. Default is TLS 1.2.</summary>
    public TlsMinimumVersion MinimumTlsVersion { get; init; } = TlsMinimumVersion.Tls12;
}

/// <summary>
/// TLS minimum version policy.
/// </summary>
public enum TlsMinimumVersion
{
    /// <summary>TLS 1.2 minimum.</summary>
    Tls12,

    /// <summary>TLS 1.3 minimum.</summary>
    Tls13,
}

/// <summary>
/// Logging configuration.
/// </summary>
public sealed class LoggingConfiguration
{
    /// <summary>Log level (Trace, Debug, Info, Warn, Error, Fatal).</summary>
    public string Level { get; init; } = "Info";

    /// <summary>Log file path.</summary>
    public string FilePath { get; init; } = "/var/log/sharpcoredb/server.log";

    /// <summary>Max log file size (MB).</summary>
    public int MaxFileSizeMB { get; init; } = 100;

    /// <summary>Max log files to retain.</summary>
    public int MaxFiles { get; init; } = 10;

    /// <summary>Log queries.</summary>
    public bool LogQueries { get; init; }

    /// <summary>Log slow queries (above threshold).</summary>
    public bool LogSlowQueries { get; init; } = true;

    /// <summary>Slow query threshold (ms).</summary>
    public int SlowQueryThresholdMs { get; init; } = 1000;
}

/// <summary>
/// Performance tuning configuration.
/// </summary>
public sealed class PerformanceConfiguration
{
    /// <summary>Query cache size (MB).</summary>
    public int QueryCacheSizeMB { get; init; } = 256;

    /// <summary>Enable query plan caching.</summary>
    public bool EnableQueryPlanCache { get; init; } = true;

    /// <summary>Worker threads (0 = auto-detect).</summary>
    public int WorkerThreads { get; init; } = 0;

    /// <summary>Enable internal metrics collection.</summary>
    public bool EnableMetrics { get; init; } = true;
}
