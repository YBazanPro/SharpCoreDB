@echo off
echo Adding SharpCoreDB.Server projects to solution...

dotnet sln add src/SharpCoreDB.Server/SharpCoreDB.Server.csproj
dotnet sln add src/SharpCoreDB.Server.Protocol/SharpCoreDB.Server.Protocol.csproj
dotnet sln add src/SharpCoreDB.Server.Core/SharpCoreDB.Server.Core.csproj
dotnet sln add src/SharpCoreDB.Client/SharpCoreDB.Client.csproj
dotnet sln add src/SharpCoreDB.Client.Protocol/SharpCoreDB.Client.Protocol.csproj

echo.
echo Building server projects to generate protobuf files...
dotnet build src/SharpCoreDB.Server.Protocol/SharpCoreDB.Server.Protocol.csproj
dotnet build src/SharpCoreDB.Client.Protocol/SharpCoreDB.Client.Protocol.csproj

echo.
echo Building core projects...
dotnet build src/SharpCoreDB.Server.Core/SharpCoreDB.Server.Core.csproj
dotnet build src/SharpCoreDB.Client/SharpCoreDB.Client.csproj

echo.
echo Building server executable...
dotnet build src/SharpCoreDB.Server/SharpCoreDB.Server.csproj

echo.
echo Done! Run with: dotnet run --project src/SharpCoreDB.Server/SharpCoreDB.Server.csproj
pause
