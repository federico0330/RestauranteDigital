using Restaurante.Application.DTOs;

namespace Restaurante.Application.Interfaces;

public interface IBranchService
{
    Task<IEnumerable<BranchResponse>> GetActiveBranchesAsync();
    Task<BranchResponse?> CreateBranchAsync(BranchRequest request);
    Task<IEnumerable<StockResponse>> GetStockByBranchAsync(int branchId);
    Task<StockResponse?> AdjustStockAsync(int branchId, Guid dishId, StockAdjustmentRequest request);
    Task<IEnumerable<StockResponse>> GetLowStockAlertsAsync();
}
