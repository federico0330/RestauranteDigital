using Microsoft.Extensions.Logging;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(DateTime? date, int? status)
    {
        var orders = await _unitOfWork.Orders.GetOrdersAsync(date, status);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(long id)
    {
        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
        return order is null ? null : MapToResponse(order);
    }

    public async Task<OrderResponse?> CreateOrderAsync(OrderRequest request)
    {
        var deliveryType = await _unitOfWork.DeliveryTypes.GetByIdAsync(request.DeliveryType);
        if (deliveryType is null) return null;

        var branch = await _unitOfWork.Branches.GetByIdAsync(request.BranchId);
        if (branch is null) return null;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                DeliveryTypeId = request.DeliveryType,
                BranchId = request.BranchId,
                DeliveryTo = request.DeliveryTo,
                Notes = request.Notes,
                OverallStatusId = 1,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                Price = 0
            };

            decimal totalPrice = 0;
            var stockAlerts = new List<(Guid DishId, int Quantity, int MinStock)>();

            foreach (var itemRequest in request.Items)
            {
                var dish = await _unitOfWork.Dishes.GetByIdAsync(itemRequest.DishId);
                if (dish is null || !dish.Available) continue;

                var orderItem = new OrderItem
                {
                    DishId = itemRequest.DishId,
                    Quantity = itemRequest.Quantity,
                    Notes = itemRequest.Notes,
                    StatusId = 1,
                    CreateDate = DateTime.UtcNow
                };
                order.OrderItems.Add(orderItem);
                totalPrice += dish.Price * itemRequest.Quantity;

                // Descuento de stock por sucursal. Si no hay registro de stock previo, se asume sin control (legacy).
                var stock = await _unitOfWork.Branches.GetStockAsync(request.BranchId, itemRequest.DishId);
                if (stock is not null)
                {
                    stock.Quantity -= itemRequest.Quantity;
                    stock.UpdateDate = DateTime.UtcNow;
                    await _unitOfWork.Branches.UpsertStockAsync(stock);
                    if (stock.IsBelowMinimum)
                        stockAlerts.Add((itemRequest.DishId, stock.Quantity, stock.MinStock));
                }
            }

            if (!order.OrderItems.Any())
            {
                await _unitOfWork.RollbackTransactionAsync();
                return null;
            }

            order.Price = totalPrice;
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Evento de dominio (loggeado): listo para enganchar a notificaciones por mail/Slack/etc.
            foreach (var alert in stockAlerts)
                _logger.LogWarning("LowStockAlertRaised: branch={Branch} dish={Dish} qty={Qty} min={Min}",
                    request.BranchId, alert.DishId, alert.Quantity, alert.MinStock);

            var savedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
            return savedOrder is null ? null : MapToResponse(savedOrder);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OrderResponse?> UpdateOrderItemStatusAsync(long orderItemId, int statusId)
    {
        var order = await _unitOfWork.Orders.GetByOrderItemIdAsync(orderItemId);
        if (order is null) return null;

        order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
        if (order is null) return null;

        var item = order.OrderItems.First(oi => oi.OrderItemId == orderItemId);
        var status = await _unitOfWork.Statuses.GetByIdAsync(statusId);
        if (status is null) return null;

        item.StatusId = statusId;
        if (order.OrderItems.All(oi => oi.StatusId == statusId))
            order.OverallStatusId = statusId;

        order.UpdateDate = DateTime.UtcNow;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        var updatedOrder = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
        return updatedOrder is null ? null : MapToResponse(updatedOrder);
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

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.OrderId,
        DeliveryTo = order.DeliveryTo,
        Notes = order.Notes,
        Price = order.Price,
        CreatedAt = order.CreateDate,
        UpdatedAt = order.UpdateDate,
        BranchId = order.BranchId,
        BranchName = order.Branch?.Name,
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
