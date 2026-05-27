using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class WholesaleOrderRepository : GenericRepository<WholesaleOrder>, IWholesaleOrderRepository
{
    public WholesaleOrderRepository(ApplicationDbContext context) : base(context) { }

    public Task<WholesaleOrder?> GetWithItemsAsync(long id) =>
        _dbSet
            .Include(o => o.Branch)
            .Include(o => o.Items)
                .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<WholesaleOrder>> GetByBranchAsync(int branchId, string? status = null)
    {
        var query = _dbSet
            .Include(o => o.Branch)
            .Include(o => o.Items)
                .ThenInclude(i => i.Dish)
            .Where(o => o.BranchId == branchId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        return await query.OrderByDescending(o => o.CreateDate).ToListAsync();
    }
}
