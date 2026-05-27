using Restaurante.Application.DTOs;

namespace Restaurante.Application.Interfaces;

public interface IWarrantyService
{
    Task<WarrantyClaimResponse?> CreateAsync(WarrantyClaimRequest request);
    Task<WarrantyClaimResponse?> ResolveAsync(long id, WarrantyClaimResolveRequest request);
    Task<IEnumerable<WarrantyClaimResponse>> GetByStatusAsync(string status);
    Task<WarrantyClaimResponse?> GetByIdAsync(long id);
}
