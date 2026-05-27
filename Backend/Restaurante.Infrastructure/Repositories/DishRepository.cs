using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class DishRepository : GenericRepository<Dish>, IDishRepository
{
    public DishRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Dish>> GetDishesAsync(string? name, int? categoryId, string? sortByPrice, bool onlyActive)
    {
        // Los soft-deleted nunca aparecen en listados públicos.
        var query = _dbSet.Include(d => d.Category).Where(d => !d.IsDeleted).AsQueryable();

        if (onlyActive)
        {
            query = query.Where(d => d.Available);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(d => d.Name.Contains(name));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(sortByPrice))
        {
            if (sortByPrice.Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(d => d.Price);
            }
            else if (sortByPrice.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(d => d.Price);
            }
        }

        return await query.ToListAsync();
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeDishId = null)
    {
        var query = _dbSet.Where(d => d.Name == name);
        if (excludeDishId.HasValue)
        {
            query = query.Where(d => d.DishId != excludeDishId.Value);
        }
        return !await query.AnyAsync();
    }

    public async Task<bool> HasAssociatedOrdersAsync(Guid dishId)
    {
        return await _context.OrderItems.AnyAsync(oi => oi.DishId == dishId);
    }
    
    public override async Task<Dish?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(d => d.Category).FirstOrDefaultAsync(d => d.DishId == (Guid)id);
    }
}
