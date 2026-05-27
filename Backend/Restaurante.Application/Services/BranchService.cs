using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _uow;

    public BranchService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<BranchResponse>> GetActiveBranchesAsync()
    {
        var branches = await _uow.Branches.GetActiveAsync();
        return branches.Select(MapBranch);
    }

    public async Task<BranchResponse?> CreateBranchAsync(BranchRequest request)
    {
        var branch = new Branch
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };
        await _uow.Branches.AddAsync(branch);
        await _uow.SaveChangesAsync();
        return MapBranch(branch);
    }

    public async Task<IEnumerable<StockResponse>> GetStockByBranchAsync(int branchId)
    {
        var stocks = await _uow.Branches.GetStocksByBranchAsync(branchId);
        return stocks.Select(MapStock);
    }

    public async Task<StockResponse?> AdjustStockAsync(int branchId, Guid dishId, StockAdjustmentRequest request)
    {
        var branch = await _uow.Branches.GetByIdAsync(branchId);
        var dish = await _uow.Dishes.GetByIdAsync(dishId);
        if (branch is null || dish is null) return null;

        var stock = await _uow.Branches.GetStockAsync(branchId, dishId) ?? new BranchDishStock
        {
            BranchId = branchId,
            DishId = dishId
        };

        stock.Quantity = request.Quantity;
        stock.MinStock = request.MinStock;
        stock.UpdateDate = DateTime.UtcNow;

        await _uow.Branches.UpsertStockAsync(stock);
        await _uow.SaveChangesAsync();

        return new StockResponse(branchId, branch.Name, dishId, dish.Name, stock.Quantity, stock.MinStock, stock.IsBelowMinimum, stock.UpdateDate);
    }

    public async Task<IEnumerable<StockResponse>> GetLowStockAlertsAsync()
    {
        var stocks = await _uow.Branches.GetLowStockAcrossNetworkAsync();
        return stocks.Select(MapStock);
    }

    private static BranchResponse MapBranch(Branch b) =>
        new(b.Id, b.Name, b.Address, b.Phone, b.IsActive);

    private static StockResponse MapStock(BranchDishStock s) =>
        new(s.BranchId, s.Branch?.Name ?? string.Empty, s.DishId, s.Dish?.Name ?? string.Empty,
            s.Quantity, s.MinStock, s.IsBelowMinimum, s.UpdateDate);
}
