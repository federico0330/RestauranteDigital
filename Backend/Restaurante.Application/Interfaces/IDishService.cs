using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurante.Application.DTOs;

namespace Restaurante.Application.Interfaces;

public interface IDishService
{
    Task<IEnumerable<DishResponse>> GetDishesAsync(string? name, int? category, string? sortByPrice, bool onlyActive);
    Task<DishResponse?> GetDishByIdAsync(Guid id);
    Task<DishResponse?> CreateDishAsync(DishRequest request);
    Task<DishResponse?> UpdateDishAsync(Guid id, DishUpdateRequest request);
    Task<bool> DeleteDishAsync(Guid id);
}
