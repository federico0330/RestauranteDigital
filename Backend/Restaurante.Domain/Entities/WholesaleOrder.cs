namespace Restaurante.Domain.Entities;

public class WholesaleOrder
{
    public long Id { get; set; }

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = WholesaleOrderStatus.Pending;
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public ICollection<WholesaleOrderItem> Items { get; set; } = new List<WholesaleOrderItem>();
}

public static class WholesaleOrderStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}
