using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;

namespace Restaurante.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrders(
        [FromQuery] DateTime? date,
        [FromQuery] int? status)
    {
        var result = await _orderService.GetOrdersAsync(date, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(long id)
    {
        var result = await _orderService.GetOrderByIdAsync(id);
        if (result == null)
            return NotFound(new ApiError { Message = "Orden no encontrada" });

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(request);
        if (result == null)
            return BadRequest(new ApiError { Message = "Datos de orden inválidos o platos no disponibles" });

        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    [HttpPut("item/{orderItemId}/status/{statusId}")]
    public async Task<ActionResult<OrderResponse>> UpdateItemStatus(long orderItemId, int statusId)
    {
        var result = await _orderService.UpdateOrderItemStatusAsync(orderItemId, statusId);
        if (result == null)
            return NotFound(new ApiError { Message = "Ítem de orden o estado no encontrado" });

        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<GenericResponse>>> GetCategories()
    {
        return Ok(await _orderService.GetCategoriesAsync());
    }

    [HttpGet("delivery-types")]
    public async Task<ActionResult<IEnumerable<GenericResponse>>> GetDeliveryTypes()
    {
        return Ok(await _orderService.GetDeliveryTypesAsync());
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<IEnumerable<GenericResponse>>> GetStatuses()
    {
        return Ok(await _orderService.GetStatusesAsync());
    }
}
