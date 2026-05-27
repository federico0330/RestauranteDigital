using Microsoft.Extensions.Logging;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class WholesaleOrderService : IWholesaleOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WholesaleOrderService> _logger;

    public WholesaleOrderService(IUnitOfWork uow, ILogger<WholesaleOrderService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<WholesaleOrderResponse?> CreateAsync(WholesaleOrderRequest request)
    {
        var branch = await _uow.Branches.GetByIdAsync(request.BranchId);
        if (branch is null) return null;
        if (request.Items.Count == 0) return null;

        var order = new WholesaleOrder
        {
            BranchId = request.BranchId,
            SupplierName = request.SupplierName,
            Status = WholesaleOrderStatus.Pending,
            Notes = request.Notes,
            CreateDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var dish = await _uow.Dishes.GetByIdAsync(item.DishId);
            if (dish is null) continue;
            order.Items.Add(new WholesaleOrderItem
            {
                DishId = item.DishId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            });
        }

        if (order.Items.Count == 0) return null;
        order.TotalCost = order.Items.Sum(i => i.Subtotal);

        await _uow.WholesaleOrders.AddAsync(order);
        await _uow.SaveChangesAsync();

        var loaded = await _uow.WholesaleOrders.GetWithItemsAsync(order.Id);
        return loaded is null ? null : Map(loaded);
    }

    public async Task<WholesaleOrderResponse?> GetByIdAsync(long id)
    {
        var order = await _uow.WholesaleOrders.GetWithItemsAsync(id);
        return order is null ? null : Map(order);
    }

    public async Task<IEnumerable<WholesaleOrderResponse>> GetByBranchAsync(int branchId, string? status = null)
    {
        var orders = await _uow.WholesaleOrders.GetByBranchAsync(branchId, status);
        return orders.Select(Map);
    }

    public async Task<WholesaleOrderResponse?> ApproveAsync(long id)
    {
        var order = await _uow.WholesaleOrders.GetWithItemsAsync(id);
        if (order is null) return null;
        if (order.Status != WholesaleOrderStatus.Pending) return null;

        order.Status = WholesaleOrderStatus.Approved;
        order.ApprovedAt = DateTime.UtcNow;
        _uow.WholesaleOrders.Update(order);
        await _uow.SaveChangesAsync();
        return Map(order);
    }

    public async Task<WholesaleOrderResponse?> MarkAsReceivedAsync(long id)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.WholesaleOrders.GetWithItemsAsync(id);
            if (order is null) return null;
            if (order.Status != WholesaleOrderStatus.Approved) return null;

            foreach (var item in order.Items)
            {
                var stock = await _uow.Branches.GetStockAsync(order.BranchId, item.DishId) ?? new BranchDishStock
                {
                    BranchId = order.BranchId,
                    DishId = item.DishId,
                    MinStock = 0
                };
                stock.Quantity += item.Quantity;
                stock.UpdateDate = DateTime.UtcNow;
                await _uow.Branches.UpsertStockAsync(stock);
            }

            order.Status = WholesaleOrderStatus.Received;
            order.ReceivedAt = DateTime.UtcNow;
            _uow.WholesaleOrders.Update(order);
            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();

            _logger.LogInformation("Pedido mayorista {OrderId} recibido en sucursal {BranchId}: +{Items} items",
                id, order.BranchId, order.Items.Count);
            return Map(order);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<WholesaleOrderResponse?> CancelAsync(long id)
    {
        var order = await _uow.WholesaleOrders.GetWithItemsAsync(id);
        if (order is null) return null;
        if (order.Status == WholesaleOrderStatus.Received) return null;

        order.Status = WholesaleOrderStatus.Cancelled;
        _uow.WholesaleOrders.Update(order);
        await _uow.SaveChangesAsync();
        return Map(order);
    }

    private static WholesaleOrderResponse Map(WholesaleOrder o) => new(
        o.Id,
        o.BranchId,
        o.Branch?.Name ?? string.Empty,
        o.SupplierName,
        o.Status,
        o.TotalCost,
        o.Notes,
        o.CreateDate,
        o.ApprovedAt,
        o.ReceivedAt,
        o.Items.Select(i => new WholesaleOrderItemResponse(
            i.Id, i.DishId, i.Dish?.Name ?? string.Empty, i.Quantity, i.UnitCost, i.Subtotal)).ToList());
}
