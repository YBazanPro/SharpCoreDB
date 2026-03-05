// <copyright file="EventStreamIdTests.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SharpCoreDB.EventSourcing.Tests;

/// <summary>
/// Unit tests for EventStreamId validation and behavior.
/// </summary>
public class EventStreamIdTests
{
    [Fact]
    public void Constructor_WithValidValue_Succeeds()
    {
        var id = new EventStreamId("stream-123");
        Assert.Equal("stream-123", id.Value);
    }

    [Fact]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new EventStreamId(null!));
        Assert.Contains("null or white space", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new EventStreamId(""));
        Assert.Contains("null or white space", ex.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceValue_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new EventStreamId("   "));
        Assert.Contains("null or white space", ex.Message);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = new EventStreamId("my-stream");
        Assert.Equal("my-stream", id.ToString());
    }

    [Fact]
    public void Equality_WithSameValue_AreEqual()
    {
        var id1 = new EventStreamId("stream");
        var id2 = new EventStreamId("stream");
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equality_WithDifferentValue_AreNotEqual()
    {
        var id1 = new EventStreamId("stream1");
        var id2 = new EventStreamId("stream2");
        Assert.NotEqual(id1, id2);
    }
}
