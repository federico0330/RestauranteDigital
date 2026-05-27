using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class DishService : IDishService
{
    private readonly IUnitOfWork _unitOfWork;

    public DishService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DishResponse>> GetDishesAsync(string? name, int? category, string? sortByPrice, bool onlyActive)
    {
        var dishes = await _unitOfWork.Dishes.GetDishesAsync(name, category, sortByPrice, onlyActive);
        return dishes.Select(MapToResponse);
    }

    public async Task<DishResponse?> GetDishByIdAsync(Guid id)
    {
        var dish = await _unitOfWork.Dishes.GetByIdAsync(id);
        return dish != null ? MapToResponse(dish) : null;
    }

    public async Task<DishResponse?> CreateDishAsync(DishRequest request)
    {
        if (!await _unitOfWork.Dishes.IsNameUniqueAsync(request.Name))
            return null;

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Category);
        if (category == null) return null;

        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.Category,
            ImageUrl = request.Image,
            Available = true,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };

        await _unitOfWork.Dishes.AddAsync(dish);
        await _unitOfWork.SaveChangesAsync();

        // Reload to get Category name
        var savedDish = await _unitOfWork.Dishes.GetByIdAsync(dish.DishId);
        return MapToResponse(savedDish!);
    }

    public async Task<DishResponse?> UpdateDishAsync(Guid id, DishUpdateRequest request)
    {
        var dish = await _unitOfWork.Dishes.GetByIdAsync(id);
        if (dish == null) return null;

        if (!await _unitOfWork.Dishes.IsNameUniqueAsync(request.Name, id))
            return null;

        var category = await _unitOfWork.Categories.GetByIdAsync(request.Category);
        if (category == null) return null;

        dish.Name = request.Name;
        dish.Description = request.Description;
        dish.Price = request.Price;
        dish.CategoryId = request.Category;
        dish.ImageUrl = request.Image;
        dish.Available = request.IsActive;
        dish.UpdateDate = DateTime.UtcNow;

        _unitOfWork.Dishes.Update(dish);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(dish);
    }

    public async Task<bool> DeleteDishAsync(Guid id)
    {
        var dish = await _unitOfWork.Dishes.GetByIdAsync(id);
        if (dish == null) return false;

        // Si el plato tiene órdenes históricas, soft delete: lo marcamos como eliminado
        // y no disponible, sin romper la integridad referencial de las órdenes pasadas.
        if (await _unitOfWork.Dishes.HasAssociatedOrdersAsync(id))
        {
            dish.IsDeleted = true;
            dish.Available = false;
            dish.UpdateDate = DateTime.UtcNow;
            _unitOfWork.Dishes.Update(dish);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        _unitOfWork.Dishes.Delete(dish);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private DishResponse MapToResponse(Dish dish)
    {
        return new DishResponse
        {
            Id = dish.DishId,
            Name = dish.Name,
            Description = dish.Description,
            Price = dish.Price,
            Image = dish.ImageUrl,
            IsActive = dish.Available,
            CreatedAt = dish.CreateDate,
            UpdatedAt = dish.UpdateDate,
            Category = new GenericResponse
            {
                Id = dish.CategoryId,
                Name = dish.Category?.Name
            }
        };
    }
}
