namespace Restaurante.Application.DTOs;

public record WarrantyClaimRequest(long OrderId, long OrderItemId, string Reason);

public record WarrantyClaimResolveRequest(bool Approve, string Resolution);

public record WarrantyClaimResponse(
    long Id,
    long OrderId,
    long OrderItemId,
    string DishName,
    string Reason,
    string Status,
    string? Resolution,
    DateTime CreatedAt,
    DateTime? ResolvedAt);
