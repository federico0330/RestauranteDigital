namespace Restaurante.Domain.Entities;

public class BranchDishStock
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public DateTime UpdateDate { get; set; }

    public bool IsBelowMinimum => Quantity < MinStock;
}
