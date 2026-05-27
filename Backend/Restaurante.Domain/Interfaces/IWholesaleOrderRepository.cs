using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IWholesaleOrderRepository : IGenericRepository<WholesaleOrder>
{
    Task<WholesaleOrder?> GetWithItemsAsync(long id);
    Task<IEnumerable<WholesaleOrder>> GetByBranchAsync(int branchId, string? status = null);
}
