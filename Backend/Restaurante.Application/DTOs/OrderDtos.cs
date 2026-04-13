using System;
using System.Collections.Generic;

namespace Restaurante.Application.DTOs;

public class OrderItemRequest
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class OrderRequest
{
    public int DeliveryType { get; set; }
    public string? DeliveryTo { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemResponse
{
    public long Id { get; set; }
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public GenericResponse Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class OrderResponse
{
    public long Id { get; set; }
    public GenericResponse DeliveryType { get; set; } = null!;
    public string? DeliveryTo { get; set; }
    public GenericResponse OverallStatus { get; set; } = null!;
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}
