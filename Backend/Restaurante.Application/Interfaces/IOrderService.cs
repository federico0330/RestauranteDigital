using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurante.Application.DTOs;

namespace Restaurante.Application.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetOrdersAsync(DateTime? date, int? status);
    Task<OrderResponse?> GetOrderByIdAsync(long id);
    Task<OrderResponse?> CreateOrderAsync(OrderRequest request);
    Task<OrderResponse?> UpdateOrderItemStatusAsync(long orderItemId, int statusId);
    Task<IEnumerable<GenericResponse>> GetCategoriesAsync();
    Task<IEnumerable<GenericResponse>> GetDeliveryTypesAsync();
    Task<IEnumerable<GenericResponse>> GetStatusesAsync();
}
