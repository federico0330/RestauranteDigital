using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IWarrantyClaimRepository : IGenericRepository<WarrantyClaim>
{
    Task<WarrantyClaim?> GetWithOrderAsync(long id);
    Task<IEnumerable<WarrantyClaim>> GetByStatusAsync(string status);
}
