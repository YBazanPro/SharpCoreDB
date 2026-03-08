// <copyright file="Program.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using BenchmarkDotNet.Running;
using SharpCoreDB.Server.Benchmarks;

Console.WriteLine("SharpCoreDB Server Performance Benchmarks");
Console.WriteLine("=========================================");

// Run all benchmarks
var summary = BenchmarkRunner.Run([
    typeof(GrpcThroughputBenchmark),
    typeof(RestApiBenchmark),
    typeof(WebSocketStreamingBenchmark),
    typeof(ConnectionPoolBenchmark)
]);

Console.WriteLine("Benchmarks completed. Results saved to BenchmarkDotNet.Artifacts folder.");
