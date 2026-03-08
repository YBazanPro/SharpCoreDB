# ── SharpCoreDB Server ──
# Multi-stage build for .NET 10 production container
# Image: sharpcoredb/server:1.5.0

# ── Stage 1: Build ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files first for layer caching
COPY Directory.Build.props Directory.Packages.props* ./
COPY *.sln ./

# Copy project files (restore layer)
COPY src/SharpCoreDB/SharpCoreDB.csproj src/SharpCoreDB/
COPY src/SharpCoreDB.Server/SharpCoreDB.Server.csproj src/SharpCoreDB.Server/
COPY src/SharpCoreDB.Server.Core/SharpCoreDB.Server.Core.csproj src/SharpCoreDB.Server.Core/
COPY src/SharpCoreDB.Server.Protocol/SharpCoreDB.Server.Protocol.csproj src/SharpCoreDB.Server.Protocol/
COPY src/SharpCoreDB.Client/SharpCoreDB.Client.csproj src/SharpCoreDB.Client/
COPY src/SharpCoreDB.Client.Protocol/SharpCoreDB.Client.Protocol.csproj src/SharpCoreDB.Client.Protocol/
COPY src/SharpCoreDB.Analytics/SharpCoreDB.Analytics.csproj src/SharpCoreDB.Analytics/
COPY src/SharpCoreDB.VectorSearch/SharpCoreDB.VectorSearch.csproj src/SharpCoreDB.VectorSearch/
COPY src/SharpCoreDB.Graph/SharpCoreDB.Graph.csproj src/SharpCoreDB.Graph/
COPY src/SharpCoreDB.Distributed/SharpCoreDB.Distributed.csproj src/SharpCoreDB.Distributed/
COPY src/SharpCoreDB.EventSourcing/SharpCoreDB.EventSourcing.csproj src/SharpCoreDB.EventSourcing/
COPY src/SharpCoreDB.EntityFrameworkCore/SharpCoreDB.EntityFrameworkCore.csproj src/SharpCoreDB.EntityFrameworkCore/
COPY src/SharpCoreDB.Provider.Sync/SharpCoreDB.Provider.Sync.csproj src/SharpCoreDB.Provider.Sync/
COPY src/SharpCoreDB.Data.Provider/SharpCoreDB.Data.Provider.csproj src/SharpCoreDB.Data.Provider/

RUN dotnet restore src/SharpCoreDB.Server/SharpCoreDB.Server.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/SharpCoreDB.Server/SharpCoreDB.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Labels
LABEL maintainer="MPCoreDeveloper" \
      org.opencontainers.image.title="SharpCoreDB Server" \
      org.opencontainers.image.version="1.5.0" \
      org.opencontainers.image.description="SharpCoreDB network database server (gRPC + HTTPS)" \
      org.opencontainers.image.source="https://github.com/MPCoreDeveloper/SharpCoreDB"

# Create non-root user
RUN groupadd --gid 1000 sharpcoredb && \
    useradd --uid 1000 --gid sharpcoredb --shell /bin/false sharpcoredb

# Create data and config directories
RUN mkdir -p /app/data/system /app/data/user /app/logs /app/certs /app/secrets && \
    chown -R sharpcoredb:sharpcoredb /app

# Copy published application
COPY --from=build /app/publish .

# Environment variables (overridable)
ENV ASPNETCORE_URLS="https://+:5001;https://+:8443" \
    ASPNETCORE_ENVIRONMENT="Production" \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=1

# Expose gRPC (5001) and HTTPS API (8443) ports
EXPOSE 5001 8443

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -fsk https://localhost:8443/health || exit 1

# Run as non-root
USER sharpcoredb

ENTRYPOINT ["dotnet", "sharpcoredb-server.dll"]
