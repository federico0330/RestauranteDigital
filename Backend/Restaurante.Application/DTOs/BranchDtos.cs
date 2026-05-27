namespace Restaurante.Application.DTOs;

public record BranchRequest(string Name, string Address, string? Phone);

public record BranchResponse(int Id, string Name, string Address, string? Phone, bool IsActive);

public record StockResponse(int BranchId, string BranchName, Guid DishId, string DishName, int Quantity, int MinStock, bool IsBelowMinimum, DateTime UpdatedAt);

public record StockAdjustmentRequest(int Quantity, int MinStock);
