using Microsoft.EntityFrameworkCore.Storage;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;
using Restaurante.Infrastructure.Persistence;

namespace Restaurante.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IDishRepository? _dishRepository;
    private IOrderRepository? _orderRepository;
    private IGenericRepository<Category>? _categoryRepository;
    private IGenericRepository<DeliveryType>? _deliveryTypeRepository;
    private IGenericRepository<Status>? _statusRepository;
    private IUserRepository? _userRepository;
    private IBranchRepository? _branchRepository;
    private IWholesaleOrderRepository? _wholesaleOrderRepository;
    private IWarrantyClaimRepository? _warrantyClaimRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IDishRepository Dishes => _dishRepository ??= new DishRepository(_context);
    public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
    public IGenericRepository<Category> Categories => _categoryRepository ??= new GenericRepository<Category>(_context);
    public IGenericRepository<DeliveryType> DeliveryTypes => _deliveryTypeRepository ??= new GenericRepository<DeliveryType>(_context);
    public IGenericRepository<Status> Statuses => _statusRepository ??= new GenericRepository<Status>(_context);
    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IBranchRepository Branches => _branchRepository ??= new BranchRepository(_context);
    public IWholesaleOrderRepository WholesaleOrders => _wholesaleOrderRepository ??= new WholesaleOrderRepository(_context);
    public IWarrantyClaimRepository WarrantyClaims => _warrantyClaimRepository ??= new WarrantyClaimRepository(_context);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null) return;
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
