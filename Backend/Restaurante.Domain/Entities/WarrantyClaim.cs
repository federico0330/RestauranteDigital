namespace Restaurante.Domain.Entities;

public class WarrantyClaim
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public long OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = WarrantyClaimStatus.Pending;
    public string? Resolution { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public static class WarrantyClaimStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
