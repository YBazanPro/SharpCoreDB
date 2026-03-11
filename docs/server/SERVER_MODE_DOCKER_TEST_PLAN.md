# SharpCoreDB Server Mode Docker Test Plan

## Goal
Validate server mode end-to-end in Docker on a local development machine with TLS enabled and multi-database startup.

## Scope
- Build and run `SharpCoreDB.Server` in Docker
- Verify HTTPS health and basic REST endpoint
- Verify gRPC endpoint reachability
- Verify configured databases (system + user) initialize correctly
- Verify TLS-only posture (no plain HTTP)

## Prerequisites
- Docker Desktop (or Docker Engine + Compose plugin)
- `pwsh`
- Repo root: `D:\source\repos\MPCoreDeveloper\SharpCoreDB`

## Test Data and Secrets Setup
1. Create server folders:
   - `src/SharpCoreDB.Server/certs`
   - `src/SharpCoreDB.Server/secrets`

2. Generate test TLS certificate (`server.pfx`) into `src/SharpCoreDB.Server/certs`.

3. Create key files expected by `appsettings.json`:
   - `src/SharpCoreDB.Server/secrets/master.key`
   - `src/SharpCoreDB.Server/secrets/model.key`
   - `src/SharpCoreDB.Server/secrets/msdb.key`
   - `src/SharpCoreDB.Server/secrets/appdb.key`

4. Ensure `Server:Security:JwtSecretKey` is overridden with a strong value for test runs.

## Execution Steps
1. Build image and start compose stack:
   - Run from `src/SharpCoreDB.Server`
   - `docker compose up -d --build`

2. Verify container health:
   - `docker ps`
   - `docker inspect --format='{{json .State.Health}}' sharpcoredb-server`

3. Verify HTTPS health endpoint:
   - `curl -k https://localhost:8443/health`
   - Expected: HTTP 200

4. Verify root endpoint:
   - `curl -k https://localhost:8443/`
   - Expected JSON with server status/version

5. Verify gRPC port open:
   - `grpcurl -insecure localhost:5001 list` (if `grpcurl` available)
   - Fallback: confirm listening port via container logs

6. Verify TLS-only posture:
   - `curl http://localhost:8443/health`
   - Expected: failure or redirect not exposing plain HTTP service

7. Verify startup logs for database initialization:
   - `docker logs sharpcoredb-server --tail 300`
   - Check for configured DB names: `master`, `model`, `msdb`, `tempdb`, `appdb`

## Functional Smoke Checks (Optional but Recommended)
1. REST auth flow (if endpoints enabled): obtain token and call protected endpoint.
2. gRPC unary query smoke test against `appdb`.
3. WebSocket handshake on configured path (`/ws`).

## Pass/Fail Criteria
- Container healthy within 2 minutes
- `/health` responds over HTTPS
- gRPC endpoint reachable on `5001`
- No plain HTTP endpoint exposure
- Server logs show all configured databases initialized without fatal errors

## Teardown
- `docker compose down -v`
- Remove temporary certs/secrets generated for local testing

## Next Step (Automation)
Create a script `scripts/test-server-docker.ps1` that:
- prepares certs/secrets
- starts compose
- executes health/gRPC/TLS checks
- prints a concise pass/fail summary
- tears down in `finally`
