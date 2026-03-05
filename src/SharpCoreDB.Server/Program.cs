// <copyright file="Program.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using SharpCoreDB.Server.Core;

// Create and configure the host
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/sharpcoredb-server-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Kestrel for gRPC and HTTPS
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var config = context.Configuration.GetSection("Server").Get<ServerConfiguration>();
    if (config is null)
    {
        throw new InvalidOperationException("Server configuration is missing");
    }

    if (!config.Security.TlsEnabled)
    {
        throw new InvalidOperationException("TLS must be enabled. Plain HTTP is not supported.");
    }

    if (string.IsNullOrWhiteSpace(config.Security.TlsCertificatePath))
    {
        throw new InvalidOperationException("TLS certificate path is required.");
    }

    var tlsVersion = config.Security.MinimumTlsVersion;
    var minimumTls = tlsVersion switch
    {
        TlsMinimumVersion.Tls12 => SslProtocols.Tls12 | SslProtocols.Tls13,
        TlsMinimumVersion.Tls13 => SslProtocols.Tls13,
        _ => throw new InvalidOperationException($"Unsupported TLS version: {tlsVersion}"),
    };

    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = minimumTls;
    });

    // gRPC endpoint
    if (config.EnableGrpc)
    {
        options.ListenAnyIP(config.GrpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
            listenOptions.UseHttps(config.Security.TlsCertificatePath, config.Security.TlsPrivateKeyPath);
        });
    }

    // HTTPS API endpoint
    if (config.EnableHttpsApi)
    {
        options.ListenAnyIP(config.HttpsApiPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps(config.Security.TlsCertificatePath, config.Security.TlsPrivateKeyPath);
        });
    }
});

// Add services
builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 100 * 1024 * 1024; // 100MB
    options.MaxSendMessageSize = 100 * 1024 * 1024;
});

builder.Services.AddGrpcReflection();

// Add server configuration
builder.Services.Configure<ServerConfiguration>(
    builder.Configuration.GetSection("Server"));

// Add core services
builder.Services.AddSingleton<NetworkServer>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Map gRPC services
// TODO: Add gRPC service implementations in Phase 1, Week 2
// app.MapGrpcService<DatabaseServiceImpl>();
// app.MapGrpcService<VectorSearchServiceImpl>();

// Enable gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Map health check endpoint
app.MapHealthChecks("/health");

// Map REST API endpoints (placeholder)
app.MapGet("/", () => new
{
    name = "SharpCoreDB Server",
    version = "1.5.0",
    status = "running",
    transport = "HTTPS/TLS only",
});

// Start the server
Log.Information("Starting SharpCoreDB Server v1.5.0");
Log.Information("gRPC endpoint: {GrpcEndpoint}", $"https://localhost:{builder.Configuration["Server:GrpcPort"] ?? "5001"}");
Log.Information("HTTPS API endpoint enabled: {EnableHttpsApi}", builder.Configuration["Server:EnableHttpsApi"] ?? "true");
if ((builder.Configuration["Server:EnableHttpsApi"] ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase))
{
    Log.Information("HTTPS API endpoint: {HttpsEndpoint}", $"https://localhost:{builder.Configuration["Server:HttpsApiPort"] ?? "8443"}");
}

try
{
    // Start the network server
    var networkServer = app.Services.GetRequiredService<NetworkServer>();
    await networkServer.StartAsync(app.Lifetime.ApplicationStopping);

    // Run the web host
    await app.RunAsync();
}
catch (InvalidOperationException ex)
{
    Log.Fatal(ex, "SharpCoreDB Server failed startup validation");
}
finally
{
    Log.Information("SharpCoreDB Server shutdown complete");
    await Log.CloseAndFlushAsync();
}
