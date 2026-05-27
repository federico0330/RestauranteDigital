using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class WholesaleOrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IWholesaleOrderRepository> _woRepo = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IDishRepository> _dishRepo = new();

    public WholesaleOrderServiceTests()
    {
        _uow.Setup(u => u.WholesaleOrders).Returns(_woRepo.Object);
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.Dishes).Returns(_dishRepo.Object);
    }

    private WholesaleOrderService BuildSut() => new(_uow.Object, NullLogger<WholesaleOrderService>.Instance);

    [Fact]
    public async Task Approve_Fails_When_Order_Is_Not_Pending()
    {
        var order = new WholesaleOrder { Id = 1, Status = WholesaleOrderStatus.Approved };
        _woRepo.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(order);

        var result = await BuildSut().ApproveAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Approve_Moves_Pending_Order_To_Approved_With_Timestamp()
    {
        var order = new WholesaleOrder { Id = 1, Status = WholesaleOrderStatus.Pending, Items = new List<WholesaleOrderItem>() };
        _woRepo.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(order);

        var result = await BuildSut().ApproveAsync(1);

        result.Should().NotBeNull();
        order.Status.Should().Be(WholesaleOrderStatus.Approved);
        order.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReceived_Fails_When_Order_Is_Not_Approved()
    {
        var order = new WholesaleOrder { Id = 1, Status = WholesaleOrderStatus.Pending, Items = new List<WholesaleOrderItem>() };
        _woRepo.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(order);

        var result = await BuildSut().MarkAsReceivedAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsReceived_Adds_Quantities_To_Branch_Stock()
    {
        var branchId = 5;
        var dishId = Guid.NewGuid();
        var item = new WholesaleOrderItem { DishId = dishId, Quantity = 50, UnitCost = 100 };
        var order = new WholesaleOrder
        {
            Id = 1,
            Status = WholesaleOrderStatus.Approved,
            BranchId = branchId,
            Items = new List<WholesaleOrderItem> { item }
        };
        var existingStock = new BranchDishStock { BranchId = branchId, DishId = dishId, Quantity = 10 };

        _woRepo.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(order);
        _branchRepo.Setup(r => r.GetStockAsync(branchId, dishId)).ReturnsAsync(existingStock);

        var result = await BuildSut().MarkAsReceivedAsync(1);

        result.Should().NotBeNull();
        existingStock.Quantity.Should().Be(60, "el pedido recibido suma al stock previo");
        order.Status.Should().Be(WholesaleOrderStatus.Received);
        order.ReceivedAt.Should().NotBeNull();
        _branchRepo.Verify(r => r.UpsertStockAsync(existingStock), Times.Once);
    }

    [Fact]
    public async Task Cancel_Refuses_To_Cancel_Already_Received_Order()
    {
        var order = new WholesaleOrder { Id = 1, Status = WholesaleOrderStatus.Received };
        _woRepo.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(order);

        var result = await BuildSut().CancelAsync(1);

        result.Should().BeNull();
        order.Status.Should().Be(WholesaleOrderStatus.Received);
    }
}
