using System;

namespace Restaurante.Domain.Entities;

public class Dish
{
    public Guid DishId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool Available { get; set; }
    
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public string? ImageUrl { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
