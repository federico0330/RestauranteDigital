using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IBranchRepository : IGenericRepository<Branch>
{
    Task<IEnumerable<Branch>> GetActiveAsync();
    Task<BranchDishStock?> GetStockAsync(int branchId, Guid dishId);
    Task<IEnumerable<BranchDishStock>> GetStocksByBranchAsync(int branchId);
    Task<IEnumerable<BranchDishStock>> GetLowStockAcrossNetworkAsync();
    Task UpsertStockAsync(BranchDishStock stock);
}
