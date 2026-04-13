using System.Threading.Tasks;
using Restaurante.Domain.Entities;

namespace Restaurante.Domain.Interfaces;

public interface IUnitOfWork
{
    IDishRepository Dishes { get; }
    IOrderRepository Orders { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<DeliveryType> DeliveryTypes { get; }
    IGenericRepository<Status> Statuses { get; }
    
    Task<int> SaveChangesAsync();
}
