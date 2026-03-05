// <copyright file="EventReadRange.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing;

/// <summary>
/// Represents an inclusive read range for stream sequences.
/// </summary>
/// <param name="FromSequence">Inclusive start sequence.</param>
/// <param name="ToSequence">Inclusive end sequence.</param>
public readonly record struct EventReadRange(long FromSequence, long ToSequence);
