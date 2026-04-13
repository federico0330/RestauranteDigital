using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IDishRepository : IGenericRepository<Dish>
{
    Task<IEnumerable<Dish>> GetDishesAsync(string? name, int? categoryId, string? sortByPrice, bool onlyActive);
    Task<bool> IsNameUniqueAsync(string name, Guid? excludeDishId = null);
    Task<bool> HasAssociatedOrdersAsync(Guid dishId);
}
