using System;
using System.Collections.Generic;

namespace Restaurante.Domain.Entities;

public class Order
{
    public long OrderId { get; set; }
    
    public int DeliveryTypeId { get; set; }
    public DeliveryType DeliveryType { get; set; } = null!;
    
    public string? DeliveryTo { get; set; }
    
    public int OverallStatusId { get; set; }
    public Status OverallStatus { get; set; } = null!;
    
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
