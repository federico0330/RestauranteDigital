using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetOrdersAsync(DateTime? date, int? statusId)
    {
        var query = _dbSet
            .Include(o => o.DeliveryType)
            .Include(o => o.OverallStatus)
            .Include(o => o.Branch)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Status)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(o => o.CreateDate.Date == date.Value.Date);
        }

        if (statusId.HasValue)
        {
            query = query.Where(o => o.OverallStatusId == statusId.Value);
        }

        return await query.OrderByDescending(o => o.CreateDate).ToListAsync();
    }

    public async Task<Order?> GetOrderWithDetailsAsync(long id)
    {
        return await _dbSet
            .Include(o => o.DeliveryType)
            .Include(o => o.OverallStatus)
            .Include(o => o.Branch)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Status)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<Order?> GetByOrderItemIdAsync(long orderItemId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderItems.Any(oi => oi.OrderItemId == orderItemId));
    }
}
