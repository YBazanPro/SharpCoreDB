// <copyright file="OrderEvents.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace OrderManagement;

using System.Text.Json;

/// <summary>
/// Base class for all order events.
/// </summary>
public abstract record OrderEvent
{
    public required string OrderId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this, GetType());
    
    public static T Deserialize<T>(ReadOnlyMemory<byte> data) where T : OrderEvent
    {
        return JsonSerializer.Deserialize<T>(data.Span) 
            ?? throw new InvalidOperationException("Failed to deserialize event");
    }
}

/// <summary>
/// Order was created with initial items.
/// </summary>
public record OrderCreatedEvent : OrderEvent
{
    public required string CustomerId { get; init; }
    public required List<OrderItem> Items { get; init; }
    public required decimal TotalAmount { get; init; }
}

/// <summary>
/// Item was added to the order.
/// </summary>
public record ItemAddedEvent : OrderEvent
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
}

/// <summary>
/// Item was removed from the order.
/// </summary>
public record ItemRemovedEvent : OrderEvent
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
}

/// <summary>
/// Order was confirmed by customer.
/// </summary>
public record OrderConfirmedEvent : OrderEvent
{
    public required decimal FinalAmount { get; init; }
}

/// <summary>
/// Payment was received for the order.
/// </summary>
public record OrderPaidEvent : OrderEvent
{
    public required decimal AmountPaid { get; init; }
    public required string PaymentMethod { get; init; }
    public required string TransactionId { get; init; }
}

/// <summary>
/// Order was shipped to customer.
/// </summary>
public record OrderShippedEvent : OrderEvent
{
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required DateTimeOffset EstimatedDelivery { get; init; }
}

/// <summary>
/// Order was delivered to customer.
/// </summary>
public record OrderDeliveredEvent : OrderEvent
{
    public required DateTimeOffset DeliveryTime { get; init; }
    public required string SignedBy { get; init; }
}

/// <summary>
/// Order was cancelled.
/// </summary>
public record OrderCancelledEvent : OrderEvent
{
    public required string Reason { get; init; }
    public required string CancelledBy { get; init; }
}

/// <summary>
/// Order item model.
/// </summary>
public record OrderItem
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
    
    public decimal Total => Quantity * Price;
}

/// <summary>
/// Order status enum.
/// </summary>
public enum OrderStatus
{
    Draft,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
