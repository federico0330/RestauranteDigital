using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IUnitOfWork
{
    IDishRepository Dishes { get; }
    IOrderRepository Orders { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<DeliveryType> DeliveryTypes { get; }
    IGenericRepository<Status> Statuses { get; }
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }
    IWholesaleOrderRepository WholesaleOrders { get; }
    IWarrantyClaimRepository WarrantyClaims { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
