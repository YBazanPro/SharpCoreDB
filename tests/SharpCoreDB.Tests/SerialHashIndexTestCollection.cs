// <copyright file="SerialHashIndexTestCollection.cs" company="MPCoreDeveloper">
// Copyright (c) 2025-2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace SharpCoreDB.Tests;

using Xunit;

/// <summary>
/// Collection definition for hash index tests that toggle the global
/// SHARPCOREDB_USE_UNSAFE_EQUALITY_INDEX environment variable.
/// Tests in this collection run serially to prevent env-var race conditions,
/// but they do NOT block the rest of the test suite (unlike PerformanceTests).
/// </summary>
[CollectionDefinition("SerialHashIndexTests", DisableParallelization = true)]
public class SerialHashIndexTestCollection;
