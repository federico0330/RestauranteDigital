using System;

namespace Restaurante.Domain.Entities;

public class OrderItem
{
    public long OrderItemId { get; set; }
    
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;
    
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    
    public DateTime CreateDate { get; set; }
}
