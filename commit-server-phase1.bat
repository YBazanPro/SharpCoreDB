@echo off
echo Committing SharpCoreDB.Server Phase 1 Week 1...

git add src/SharpCoreDB.Server/
git add src/SharpCoreDB.Server.Protocol/
git add src/SharpCoreDB.Server.Core/
git add src/SharpCoreDB.Client/
git add src/SharpCoreDB.Client.Protocol/
git add build-server.bat
git add docs/server/PHASE1_WEEK1_COMPLETE.md
git add docs/server/COMBINED_ROADMAP_V1.5_V1.6.md

echo.
git commit -m "feat(server): Phase 1 Week 1 foundation - 5 projects with gRPC protocol

PHASE 1 WEEK 1 COMPLETE:

NEW PROJECTS (5):
- SharpCoreDB.Server (.NET 10 executable with Kestrel + gRPC)
- SharpCoreDB.Server.Protocol (gRPC .proto definitions)
- SharpCoreDB.Server.Core (NetworkServer, Configuration)
- SharpCoreDB.Client (ADO.NET-like client library)
- SharpCoreDB.Client.Protocol (Client-side protocol)

FEATURES:
- gRPC DatabaseService with 8 RPCs (query, transaction, health)
- gRPC VectorSearchService (semantic search)
- NetworkServer orchestrator (connection management)
- ServerConfiguration (comprehensive config models)
- SharpCoreDBConnection (ADO.NET-like API)
- SharpCoreDBCommand (query execution)
- SharpCoreDBTransaction (ACID support)
- Program.cs with Kestrel HTTP/2 setup

C# 14 FEATURES USED:
- Primary constructors (DI injection)
- Lock class (not object)
- Collection expressions []
- Required properties
- Nullable reference types

BUILD STATUS: ✅ All projects compile successfully
PROTOBUF: ✅ Code generation working

NEXT: Week 2 - Implement gRPC service handlers + authentication"

echo.
echo Pushing to GitHub...
git push origin master

echo.
echo Done!
pause
