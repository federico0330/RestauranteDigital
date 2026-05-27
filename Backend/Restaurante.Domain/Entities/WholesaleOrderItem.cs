namespace Restaurante.Domain.Entities;

public class WholesaleOrderItem
{
    public long Id { get; set; }

    public long WholesaleOrderId { get; set; }
    public WholesaleOrder WholesaleOrder { get; set; } = null!;

    public Guid DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public decimal Subtotal => Quantity * UnitCost;
}
