using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Services;

public class WarrantyService : IWarrantyService
{
    private readonly IUnitOfWork _uow;

    public WarrantyService(IUnitOfWork uow) => _uow = uow;

    public async Task<WarrantyClaimResponse?> CreateAsync(WarrantyClaimRequest request)
    {
        var order = await _uow.Orders.GetOrderWithDetailsAsync(request.OrderId);
        if (order is null) return null;

        var item = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == request.OrderItemId);
        if (item is null) return null;

        var claim = new WarrantyClaim
        {
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            Reason = request.Reason,
            Status = WarrantyClaimStatus.Pending,
            CreateDate = DateTime.UtcNow
        };

        await _uow.WarrantyClaims.AddAsync(claim);
        await _uow.SaveChangesAsync();

        var loaded = await _uow.WarrantyClaims.GetWithOrderAsync(claim.Id);
        return loaded is null ? null : Map(loaded);
    }

    public async Task<WarrantyClaimResponse?> ResolveAsync(long id, WarrantyClaimResolveRequest request)
    {
        var claim = await _uow.WarrantyClaims.GetWithOrderAsync(id);
        if (claim is null) return null;
        if (claim.Status != WarrantyClaimStatus.Pending) return null;

        claim.Status = request.Approve ? WarrantyClaimStatus.Approved : WarrantyClaimStatus.Rejected;
        claim.Resolution = request.Resolution;
        claim.ResolvedAt = DateTime.UtcNow;

        _uow.WarrantyClaims.Update(claim);
        await _uow.SaveChangesAsync();
        return Map(claim);
    }

    public async Task<IEnumerable<WarrantyClaimResponse>> GetByStatusAsync(string status)
    {
        var claims = await _uow.WarrantyClaims.GetByStatusAsync(status);
        return claims.Select(Map);
    }

    public async Task<WarrantyClaimResponse?> GetByIdAsync(long id)
    {
        var claim = await _uow.WarrantyClaims.GetWithOrderAsync(id);
        return claim is null ? null : Map(claim);
    }

    private static WarrantyClaimResponse Map(WarrantyClaim c) => new(
        c.Id,
        c.OrderId,
        c.OrderItemId,
        c.OrderItem?.Dish?.Name ?? string.Empty,
        c.Reason,
        c.Status,
        c.Resolution,
        c.CreateDate,
        c.ResolvedAt);
}
