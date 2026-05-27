using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class BranchRepository : GenericRepository<Branch>, IBranchRepository
{
    public BranchRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Branch>> GetActiveAsync() =>
        await _dbSet.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

    public Task<BranchDishStock?> GetStockAsync(int branchId, Guid dishId) =>
        _context.BranchDishStocks
            .Include(s => s.Branch)
            .Include(s => s.Dish)
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.DishId == dishId);

    public async Task<IEnumerable<BranchDishStock>> GetStocksByBranchAsync(int branchId) =>
        await _context.BranchDishStocks
            .Include(s => s.Branch)
            .Include(s => s.Dish)
            .Where(s => s.BranchId == branchId)
            .OrderBy(s => s.Dish.Name)
            .ToListAsync();

    public async Task<IEnumerable<BranchDishStock>> GetLowStockAcrossNetworkAsync() =>
        await _context.BranchDishStocks
            .Include(s => s.Branch)
            .Include(s => s.Dish)
            .Where(s => s.Quantity < s.MinStock)
            .ToListAsync();

    public async Task UpsertStockAsync(BranchDishStock stock)
    {
        var existing = await _context.BranchDishStocks
            .FirstOrDefaultAsync(s => s.BranchId == stock.BranchId && s.DishId == stock.DishId);

        if (existing is null)
        {
            await _context.BranchDishStocks.AddAsync(stock);
        }
        else
        {
            existing.Quantity = stock.Quantity;
            existing.MinStock = stock.MinStock;
            existing.UpdateDate = stock.UpdateDate;
            _context.BranchDishStocks.Update(existing);
        }
    }
}
