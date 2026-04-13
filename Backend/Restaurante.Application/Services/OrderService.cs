using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(DateTime? date, int? status)
    {
        var orders = await _unitOfWork.Orders.GetOrdersAsync(date, status);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(long id)
    {
        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
        return order != null ? MapToResponse(order) : null;
    }

    public async Task<OrderResponse?> CreateOrderAsync(OrderRequest request)
    {
        var deliveryType = await _unitOfWork.DeliveryTypes.GetByIdAsync(request.DeliveryType);
        if (deliveryType == null) return null;

        var order = new Order
        {
            DeliveryTypeId = request.DeliveryType,
            DeliveryTo = request.DeliveryTo,
            Notes = request.Notes,
            OverallStatusId = 1, // Pending
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            Price = 0
        };

        decimal totalPrice = 0;
        foreach (var itemRequest in request.Items)
        {
            var dish = await _unitOfWork.Dishes.GetByIdAsync(itemRequest.DishId);
            if (dish == null || !dish.Available) continue;

            var orderItem = new OrderItem
            {
                DishId = itemRequest.DishId,
                Quantity = itemRequest.Quantity,
                Notes = itemRequest.Notes,
                StatusId = 1, // Pending
                CreateDate = DateTime.UtcNow
            };
            order.OrderItems.Add(orderItem);
            totalPrice += dish.Price * itemRequest.Quantity;
        }

        if (!order.OrderItems.Any()) return null;

        order.Price = totalPrice;
        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var savedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
        return MapToResponse(savedOrder!);
    }

    public async Task<OrderResponse?> UpdateOrderItemStatusAsync(long orderItemId, int statusId)
    {
        var order = await _unitOfWork.Orders.GetByOrderItemIdAsync(orderItemId);
        
        if (order == null) return null;
        
        // Ensure all details are loaded for the response and for the All() check
        order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
        if (order == null) return null;

        var item = order.OrderItems.First(oi => oi.OrderItemId == orderItemId);
        var status = await _unitOfWork.Statuses.GetByIdAsync(statusId);
        if (status == null) return null;

        item.StatusId = statusId;
        
        // Logic: "La transición de estados de la orden depende del estado de sus ítem, 
        // solo si todos los ítems de la orden transicionan a un mismo estado, 
        // la orden debe transicionar de estado."
        if (order.OrderItems.All(oi => oi.StatusId == statusId))
        {
            order.OverallStatusId = statusId;
        }

        order.UpdateDate = DateTime.UtcNow;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        // Re-fetch to get updated navigation properties (Status names, etc.)
        var updatedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
        return MapToResponse(updatedOrder!);
    }

    public async Task<IEnumerable<GenericResponse>> GetCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return categories.OrderBy(c => c.Order).Select(c => new GenericResponse { Id = c.Id, Name = c.Name });
    }

    public async Task<IEnumerable<GenericResponse>> GetDeliveryTypesAsync()
    {
        var types = await _unitOfWork.DeliveryTypes.GetAllAsync();
        return types.Select(t => new GenericResponse { Id = t.Id, Name = t.Name });
    }

    public async Task<IEnumerable<GenericResponse>> GetStatusesAsync()
    {
        var statuses = await _unitOfWork.Statuses.GetAllAsync();
        return statuses.Select(s => new GenericResponse { Id = s.Id, Name = s.Name });
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.OrderId,
            DeliveryTo = order.DeliveryTo,
            Notes = order.Notes,
            Price = order.Price,
            CreatedAt = order.CreateDate,
            UpdatedAt = order.UpdateDate,
            DeliveryType = new GenericResponse { Id = order.DeliveryTypeId, Name = order.DeliveryType?.Name },
            OverallStatus = new GenericResponse { Id = order.OverallStatusId, Name = order.OverallStatus?.Name },
            Items = order.OrderItems.Select(oi => new OrderItemResponse
            {
                Id = oi.OrderItemId,
                DishId = oi.DishId,
                DishName = oi.Dish?.Name ?? "Unknown",
                Quantity = oi.Quantity,
                Notes = oi.Notes,
                CreatedAt = oi.CreateDate,
                Status = new GenericResponse { Id = oi.StatusId, Name = oi.Status?.Name }
            }).ToList()
        };
    }
}
