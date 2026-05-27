using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class WarrantyClaimRepository : GenericRepository<WarrantyClaim>, IWarrantyClaimRepository
{
    public WarrantyClaimRepository(ApplicationDbContext context) : base(context) { }

    public Task<WarrantyClaim?> GetWithOrderAsync(long id) =>
        _dbSet
            .Include(c => c.Order)
            .Include(c => c.OrderItem)
                .ThenInclude(oi => oi.Dish)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<WarrantyClaim>> GetByStatusAsync(string status) =>
        await _dbSet
            .Include(c => c.OrderItem)
                .ThenInclude(oi => oi.Dish)
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreateDate)
            .ToListAsync();
}
