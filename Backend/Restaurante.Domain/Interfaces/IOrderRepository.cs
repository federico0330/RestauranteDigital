using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersAsync(DateTime? date, int? statusId);
    Task<Order?> GetOrderWithDetailsAsync(long id);
    Task<Order?> GetByOrderItemIdAsync(long orderItemId);
}
