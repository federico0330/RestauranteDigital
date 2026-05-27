namespace Restaurante.Application.DTOs;

public record WholesaleOrderItemRequest(Guid DishId, int Quantity, decimal UnitCost);

public record WholesaleOrderRequest(int BranchId, string SupplierName, string? Notes, List<WholesaleOrderItemRequest> Items);

public record WholesaleOrderItemResponse(long Id, Guid DishId, string DishName, int Quantity, decimal UnitCost, decimal Subtotal);

public record WholesaleOrderResponse(
    long Id,
    int BranchId,
    string BranchName,
    string SupplierName,
    string Status,
    decimal TotalCost,
    string? Notes,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? ReceivedAt,
    List<WholesaleOrderItemResponse> Items);
