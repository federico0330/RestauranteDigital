using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IDishRepository> _dishRepo = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IGenericRepository<DeliveryType>> _deliveryRepo = new();

    public OrderServiceTests()
    {
        _uow.Setup(u => u.Orders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.Dishes).Returns(_dishRepo.Object);
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.DeliveryTypes).Returns(_deliveryRepo.Object);
    }

    private OrderService BuildSut() => new(_uow.Object, NullLogger<OrderService>.Instance);

    [Fact]
    public async Task CreateOrder_Returns_Null_When_Branch_Does_Not_Exist()
    {
        _deliveryRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new DeliveryType { Id = 1 });
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((Branch?)null);

        var result = await BuildSut().CreateOrderAsync(new OrderRequest
        {
            DeliveryType = 1, BranchId = 999, Items = new() { new OrderItemRequest { DishId = Guid.NewGuid(), Quantity = 1 } }
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_Decreases_Branch_Stock_For_Each_Item()
    {
        var branchId = 1;
        var dishId = Guid.NewGuid();
        var dish = new Dish { DishId = dishId, Name = "Pizza", Price = 100, Available = true };
        var stock = new BranchDishStock { BranchId = branchId, DishId = dishId, Quantity = 20, MinStock = 5 };

        _deliveryRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new DeliveryType { Id = 1 });
        _branchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new Branch { Id = branchId, Name = "Central" });
        _dishRepo.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
        _branchRepo.Setup(r => r.GetStockAsync(branchId, dishId)).ReturnsAsync(stock);
        _orderRepo.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<long>()))
                  .ReturnsAsync(new Order { OrderId = 1, BranchId = branchId, OrderItems = new List<OrderItem>() });

        await BuildSut().CreateOrderAsync(new OrderRequest
        {
            DeliveryType = 1, BranchId = branchId,
            Items = new() { new OrderItemRequest { DishId = dishId, Quantity = 3 } }
        });

        stock.Quantity.Should().Be(17, "se descontaron 3 unidades del stock de la sucursal");
        _branchRepo.Verify(r => r.UpsertStockAsync(stock), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_Without_Branch_Stock_Record_Does_Not_Block_Creation()
    {
        var branchId = 1;
        var dishId = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new DeliveryType { Id = 1 });
        _branchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new Branch { Id = branchId, Name = "Central" });
        _dishRepo.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(new Dish { DishId = dishId, Name = "X", Price = 10, Available = true });
        _branchRepo.Setup(r => r.GetStockAsync(branchId, dishId)).ReturnsAsync((BranchDishStock?)null);
        _orderRepo.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<long>()))
                  .ReturnsAsync(new Order { OrderId = 1, BranchId = branchId, OrderItems = new List<OrderItem>() });

        var result = await BuildSut().CreateOrderAsync(new OrderRequest
        {
            DeliveryType = 1, BranchId = branchId,
            Items = new() { new OrderItemRequest { DishId = dishId, Quantity = 2 } }
        });

        result.Should().NotBeNull();
        _branchRepo.Verify(r => r.UpsertStockAsync(It.IsAny<BranchDishStock>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_Rolls_Back_When_No_Valid_Items()
    {
        var branchId = 1;
        var dishId = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new DeliveryType { Id = 1 });
        _branchRepo.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new Branch { Id = branchId });
        _dishRepo.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync((Dish?)null);

        var result = await BuildSut().CreateOrderAsync(new OrderRequest
        {
            DeliveryType = 1, BranchId = branchId,
            Items = new() { new OrderItemRequest { DishId = dishId, Quantity = 2 } }
        });

        result.Should().BeNull();
        _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }
}
