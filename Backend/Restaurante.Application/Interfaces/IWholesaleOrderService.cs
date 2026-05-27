using Restaurante.Application.DTOs;

namespace Restaurante.Application.Interfaces;

public interface IWholesaleOrderService
{
    Task<WholesaleOrderResponse?> CreateAsync(WholesaleOrderRequest request);
    Task<WholesaleOrderResponse?> GetByIdAsync(long id);
    Task<IEnumerable<WholesaleOrderResponse>> GetByBranchAsync(int branchId, string? status = null);
    Task<WholesaleOrderResponse?> ApproveAsync(long id);
    Task<WholesaleOrderResponse?> MarkAsReceivedAsync(long id);
    Task<WholesaleOrderResponse?> CancelAsync(long id);
}
