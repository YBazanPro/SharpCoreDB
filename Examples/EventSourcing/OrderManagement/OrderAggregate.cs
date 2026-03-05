// <copyright file="OrderAggregate.cs" company="MPCoreDeveloper">
// Copyright (c) 2026 MPCoreDeveloper and GitHub Copilot. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace OrderManagement;

using SharpCoreDB.EventSourcing;

/// <summary>
/// Order aggregate that reconstructs state from events.
/// Demonstrates event sourcing pattern with SharpCoreDB.EventSourcing.
/// </summary>
public class OrderAggregate
{
    // Current state (rebuilt from events)
    public string OrderId { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public List<OrderItem> Items { get; private set; } = [];
    public decimal TotalAmount { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    
    // Event sourcing metadata
    public long Version { get; private set; }
    public List<OrderEvent> PendingEvents { get; private set; } = [];

    /// <summary>
    /// Creates a new order (command).
    /// </summary>
    public static OrderAggregate CreateOrder(string orderId, string customerId, List<OrderItem> items)
    {
        var aggregate = new OrderAggregate();
        var orderCreated = new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            Items = items,
            TotalAmount = items.Sum(i => i.Total)
        };
        
        aggregate.Apply(orderCreated);
        aggregate.PendingEvents.Add(orderCreated);
        return aggregate;
    }

    /// <summary>
    /// Adds an item to the order (command).
    /// </summary>
    public void AddItem(string productId, string productName, int quantity, decimal price)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add items to order in {Status} status");
        }

        var itemAdded = new ItemAddedEvent
        {
            OrderId = OrderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            Price = price
        };
        
        Apply(itemAdded);
        PendingEvents.Add(itemAdded);
    }

    /// <summary>
    /// Removes an item from the order (command).
    /// </summary>
    public void RemoveItem(string productId, int quantity)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot remove items from order in {Status} status");
        }

        var itemRemoved = new ItemRemovedEvent
        {
            OrderId = OrderId,
            ProductId = productId,
            Quantity = quantity
        };
        
        Apply(itemRemoved);
        PendingEvents.Add(itemRemoved);
    }

    /// <summary>
    /// Confirms the order (command).
    /// </summary>
    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");
        }

        if (Items.Count == 0)
        {
            throw new InvalidOperationException("Cannot confirm order with no items");
        }

        var orderConfirmed = new OrderConfirmedEvent
        {
            OrderId = OrderId,
            FinalAmount = TotalAmount
        };
        
        Apply(orderConfirmed);
        PendingEvents.Add(orderConfirmed);
    }

    /// <summary>
    /// Marks order as paid (command).
    /// </summary>
    public void MarkAsPaid(decimal amount, string paymentMethod, string transactionId)
    {
        if (Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot pay order in {Status} status");
        }

        var orderPaid = new OrderPaidEvent
        {
            OrderId = OrderId,
            AmountPaid = amount,
            PaymentMethod = paymentMethod,
            TransactionId = transactionId
        };
        
        Apply(orderPaid);
        PendingEvents.Add(orderPaid);
    }

    /// <summary>
    /// Ships the order (command).
    /// </summary>
    public void ShipOrder(string trackingNumber, string carrier, DateTimeOffset estimatedDelivery)
    {
        if (Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot ship order in {Status} status");
        }

        var orderShipped = new OrderShippedEvent
        {
            OrderId = OrderId,
            TrackingNumber = trackingNumber,
            Carrier = carrier,
            EstimatedDelivery = estimatedDelivery
        };
        
        Apply(orderShipped);
        PendingEvents.Add(orderShipped);
    }

    /// <summary>
    /// Marks order as delivered (command).
    /// </summary>
    public void MarkAsDelivered(string signedBy)
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot deliver order in {Status} status");
        }

        var orderDelivered = new OrderDeliveredEvent
        {
            OrderId = OrderId,
            DeliveryTime = DateTimeOffset.UtcNow,
            SignedBy = signedBy
        };
        
        Apply(orderDelivered);
        PendingEvents.Add(orderDelivered);
    }

    /// <summary>
    /// Cancels the order (command).
    /// </summary>
    public void CancelOrder(string reason, string cancelledBy)
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot cancel delivered order");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled");
        }

        var orderCancelled = new OrderCancelledEvent
        {
            OrderId = OrderId,
            Reason = reason,
            CancelledBy = cancelledBy
        };
        
        Apply(orderCancelled);
        PendingEvents.Add(orderCancelled);
    }

    /// <summary>
    /// Rebuilds aggregate state from event stream.
    /// </summary>
    public static OrderAggregate FromEventStream(IReadOnlyList<EventEnvelope> events)
    {
        var aggregate = new OrderAggregate();
        
        foreach (var envelope in events)
        {
            var orderEvent = DeserializeEvent(envelope);
            aggregate.Apply(orderEvent);
            aggregate.Version = envelope.Sequence;
        }
        
        return aggregate;
    }

    /// <summary>
    /// Applies an event to update aggregate state.
    /// This is the heart of event sourcing - state is derived from events.
    /// </summary>
    private void Apply(OrderEvent orderEvent)
    {
        switch (orderEvent)
        {
            case OrderCreatedEvent created:
                OrderId = created.OrderId;
                CustomerId = created.CustomerId;
                Items = new List<OrderItem>(created.Items);
                TotalAmount = created.TotalAmount;
                Status = OrderStatus.Draft;
                break;

            case ItemAddedEvent itemAdded:
                var existingItem = Items.FirstOrDefault(i => i.ProductId == itemAdded.ProductId);
                if (existingItem != null)
                {
                    Items.Remove(existingItem);
                    Items.Add(existingItem with { Quantity = existingItem.Quantity + itemAdded.Quantity });
                }
                else
                {
                    Items.Add(new OrderItem
                    {
                        ProductId = itemAdded.ProductId,
                        ProductName = itemAdded.ProductName,
                        Quantity = itemAdded.Quantity,
                        Price = itemAdded.Price
                    });
                }
                TotalAmount = Items.Sum(i => i.Total);
                break;

            case ItemRemovedEvent itemRemoved:
                var itemToRemove = Items.FirstOrDefault(i => i.ProductId == itemRemoved.ProductId);
                if (itemToRemove != null)
                {
                    if (itemToRemove.Quantity <= itemRemoved.Quantity)
                    {
                        Items.Remove(itemToRemove);
                    }
                    else
                    {
                        Items.Remove(itemToRemove);
                        Items.Add(itemToRemove with { Quantity = itemToRemove.Quantity - itemRemoved.Quantity });
                    }
                    TotalAmount = Items.Sum(i => i.Total);
                }
                break;

            case OrderConfirmedEvent:
                Status = OrderStatus.Confirmed;
                break;

            case OrderPaidEvent paid:
                Status = OrderStatus.Paid;
                PaymentTransactionId = paid.TransactionId;
                break;

            case OrderShippedEvent shipped:
                Status = OrderStatus.Shipped;
                TrackingNumber = shipped.TrackingNumber;
                break;

            case OrderDeliveredEvent delivered:
                Status = OrderStatus.Delivered;
                DeliveredAt = delivered.DeliveryTime;
                break;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                break;
        }
    }

    /// <summary>
    /// Clears pending events after they're persisted.
    /// </summary>
    public void ClearPendingEvents()
    {
        PendingEvents.Clear();
    }

    private static OrderEvent DeserializeEvent(EventEnvelope envelope)
    {
        return envelope.EventType switch
        {
            nameof(OrderCreatedEvent) => OrderEvent.Deserialize<OrderCreatedEvent>(envelope.Payload),
            nameof(ItemAddedEvent) => OrderEvent.Deserialize<ItemAddedEvent>(envelope.Payload),
            nameof(ItemRemovedEvent) => OrderEvent.Deserialize<ItemRemovedEvent>(envelope.Payload),
            nameof(OrderConfirmedEvent) => OrderEvent.Deserialize<OrderConfirmedEvent>(envelope.Payload),
            nameof(OrderPaidEvent) => OrderEvent.Deserialize<OrderPaidEvent>(envelope.Payload),
            nameof(OrderShippedEvent) => OrderEvent.Deserialize<OrderShippedEvent>(envelope.Payload),
            nameof(OrderDeliveredEvent) => OrderEvent.Deserialize<OrderDeliveredEvent>(envelope.Payload),
            nameof(OrderCancelledEvent) => OrderEvent.Deserialize<OrderCancelledEvent>(envelope.Payload),
            _ => throw new InvalidOperationException($"Unknown event type: {envelope.EventType}")
        };
    }
}
