using System;
using System.Threading.Tasks;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;
    private IDishRepository? _dishRepository;
    private IOrderRepository? _orderRepository;
    private IGenericRepository<Category>? _categoryRepository;
    private IGenericRepository<DeliveryType>? _deliveryTypeRepository;
    private IGenericRepository<Status>? _statusRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IDishRepository Dishes => _dishRepository ??= new DishRepository(_context);
    public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
    public IGenericRepository<Category> Categories => _categoryRepository ??= new GenericRepository<Category>(_context);
    public IGenericRepository<DeliveryType> DeliveryTypes => _deliveryTypeRepository ??= new GenericRepository<DeliveryType>(_context);
    public IGenericRepository<Status> Statuses => _statusRepository ??= new GenericRepository<Status>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
