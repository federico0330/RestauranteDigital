using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;

namespace Restaurante.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DishController : ControllerBase
{
    private readonly IDishService _dishService;

    public DishController(IDishService dishService)
    {
        _dishService = dishService;
    }

    [HttpPost]
    public async Task<ActionResult<DishResponse>> CreateDish([FromBody] DishRequest request)
    {
        if (request.Price <= 0)
            return BadRequest(new ApiError { Message = "El precio debe ser mayor a cero" });

        var result = await _dishService.CreateDishAsync(request);
        if (result == null)
            return Conflict(new ApiError { Message = "Ya existe un plato con ese nombre o los datos son inválidos" });

        return CreatedAtAction(nameof(GetDish), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DishResponse>>> GetDishes(
        [FromQuery] string? name,
        [FromQuery] int? category,
        [FromQuery] string? sortByPrice,
        [FromQuery] bool onlyActive = true)
    {
        if (!string.IsNullOrEmpty(sortByPrice) && 
            !sortByPrice.Equals("asc", StringComparison.OrdinalIgnoreCase) && 
            !sortByPrice.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiError { Message = "Parámetros de ordenamiento inválidos" });
        }

        var result = await _dishService.GetDishesAsync(name, category, sortByPrice, onlyActive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DishResponse>> GetDish(Guid id)
    {
        var result = await _dishService.GetDishByIdAsync(id);
        if (result == null)
            return NotFound(new ApiError { Message = "Plato no encontrado" });

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DishResponse>> UpdateDish(Guid id, [FromBody] DishUpdateRequest request)
    {
        if (request.Price <= 0)
            return BadRequest(new ApiError { Message = "El precio debe ser mayor a cero" });

        var result = await _dishService.UpdateDishAsync(id, request);
        if (result == null)
        {
            // Simple logic: check if it exists first
            var exists = await _dishService.GetDishByIdAsync(id);
            if (exists == null)
                return NotFound(new ApiError { Message = "Plato no encontrado" });
            
            return Conflict(new ApiError { Message = "Ya existe un plato con ese nombre" });
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDish(Guid id)
    {
        var result = await _dishService.DeleteDishAsync(id);
        if (!result)
        {
            var exists = await _dishService.GetDishByIdAsync(id);
            if (exists == null)
                return NotFound(new ApiError { Message = "Plato no encontrado" });

            return BadRequest(new ApiError { Message = "El plato no puede ser eliminado si existe una orden que dependa de esta." });
        }

        return NoContent();
    }
}
