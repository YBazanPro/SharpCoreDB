# SharpCoreDB Server System Databases and Security Model

## Current Implementation Status (March 2026)

This section is the authoritative progress snapshot for this document scope.

### In Progress (What we are doing now)
- Phase 1 foundation work for server multi-database hosting.
- Configuration-first validation and hardening for secure startup behavior.
- Planning and sequencing follow-up implementation for runtime routing and system database initialization.

### Completed (What is done)
- Multi-database hosting model is defined in configuration.
- Database entry contract is defined (`Name`, `DatabasePath`, `StorageMode`, encryption, pool size, system/read-only flags).
- Startup validation rules are defined and enforced at startup:
  1. At least one database must be configured.
  2. `DefaultDatabase` must exist in `Databases`.
  3. Database names must be unique (case-insensitive).
  4. If system databases are enabled, all configured system database names must exist.
- System database responsibilities are documented (`master`, `model`, `msdb`, `tempdb`).
- HTTPS/TLS-only security posture is defined and enforced:
  - Plain HTTP is not supported.
  - TLS must be enabled.
  - Certificate path is required.
  - Minimum TLS policy supports `Tls12`/`Tls13`.
- Kestrel endpoint policy is defined:
  - gRPC over HTTPS (HTTP/2).
  - Optional management API over HTTPS only.

### Not Completed Yet (Not done)
- Runtime database routing across all requests and sessions.
- Automatic schema/bootstrap initialization for system databases.
- Per-database authorization model and policy enforcement.
- Full operational behaviors for `msdb` jobs/schedules automation.
- Full `tempdb` lifecycle management policies (creation/reset/cleanup semantics).
- End-to-end production hardening and complete implementation of all follow-up phase items.

### Out of Scope for This Phase
- Event sourcing implementation (tracked separately as optional package work).
- Benchmarking program against BLite/Zvec (tracked separately).

---

## Overview

SharpCoreDB Server supports hosting multiple databases in a single server process, including optional system databases inspired by SQL Server conventions.

This document describes:
- multi-database hosting model,
- system database responsibilities,
- mandatory HTTPS/TLS security posture.

## Multi-Database Hosting

Server configuration uses:
- `DefaultDatabase`: fallback database name when client does not specify one.
- `Databases`: explicit list of hosted database instances.

Each database entry supports:
- `Name` (logical database name)
- `DatabasePath` (physical storage path)
- `StorageMode`
- `EncryptionEnabled` + `EncryptionKeyFile`
- `ConnectionPoolSize`
- `IsSystemDatabase`
- `IsReadOnly`

Validation rules enforced at startup:
1. At least one database must be configured.
2. `DefaultDatabase` must exist in `Databases`.
3. Database names must be unique (case-insensitive).
4. If system databases are enabled, all configured system database names must exist.

## System Databases

When `SystemDatabases.Enabled = true`, the following logical system databases are expected:

- `master`: server-level metadata and global catalog.
- `model`: template database used as baseline metadata/profile for new databases.
- `msdb`: operational metadata, jobs, schedules, and automation state.
- `tempdb`: temporary objects and transient workloads.

These are configurable by name through:
- `MasterDatabaseName`
- `ModelDatabaseName`
- `MsdbDatabaseName`
- `TempDbDatabaseName`

## Security Policy (HTTPS/TLS Only)

SharpCoreDB Server is configured for secure transport only:

1. Plain HTTP endpoints are not supported.
2. TLS must be enabled (`Security.TlsEnabled = true`).
3. A TLS certificate path is required.
4. Minimum TLS version policy is enforced through `Security.MinimumTlsVersion`:
   - `Tls12` (default; allows TLS 1.2 and TLS 1.3)
   - `Tls13` (strict)

Kestrel endpoint policy:
- gRPC endpoint runs over HTTPS with HTTP/2.
- Optional management API endpoint runs over HTTPS only.

## Recommended Production Defaults

- `RequireAuthentication = true`
- `AuthMethods = ["jwt", "certificate"]`
- `TlsEnabled = true`
- `MinimumTlsVersion = "Tls12"` or `"Tls13"`
- Encrypt all persistent databases except `tempdb` if temporary-only workload policy allows plaintext.

## Notes

This is the foundational model for Phase 1. Runtime database routing, system-db schema initialization, and authorization per database will be implemented in follow-up phases.
