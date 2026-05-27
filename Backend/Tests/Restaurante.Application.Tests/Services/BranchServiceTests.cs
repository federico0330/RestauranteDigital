using FluentAssertions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class BranchServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IDishRepository> _dishRepo = new();

    public BranchServiceTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.Dishes).Returns(_dishRepo.Object);
    }

    private BranchService BuildSut() => new(_uow.Object);

    [Fact]
    public async Task AdjustStock_Returns_Null_When_Branch_Or_Dish_Not_Found()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((Branch?)null);
        _dishRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Dish());

        var result = await BuildSut().AdjustStockAsync(1, Guid.NewGuid(), new StockAdjustmentRequest(10, 5));

        result.Should().BeNull();
    }

    [Fact]
    public async Task AdjustStock_Upserts_New_Stock_When_None_Exists()
    {
        var dishId = Guid.NewGuid();
        _branchRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Branch { Id = 1, Name = "Central" });
        _dishRepo.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(new Dish { DishId = dishId, Name = "X" });
        _branchRepo.Setup(r => r.GetStockAsync(1, dishId)).ReturnsAsync((BranchDishStock?)null);

        BranchDishStock? captured = null;
        _branchRepo.Setup(r => r.UpsertStockAsync(It.IsAny<BranchDishStock>()))
                   .Callback<BranchDishStock>(s => captured = s)
                   .Returns(Task.CompletedTask);

        var result = await BuildSut().AdjustStockAsync(1, dishId, new StockAdjustmentRequest(20, 5));

        result.Should().NotBeNull();
        captured!.Quantity.Should().Be(20);
        captured.MinStock.Should().Be(5);
        captured.BranchId.Should().Be(1);
    }

    [Fact]
    public async Task GetLowStockAlerts_Returns_All_Stocks_Below_Their_Minimum()
    {
        var stocks = new List<BranchDishStock>
        {
            new() { BranchId = 1, DishId = Guid.NewGuid(), Quantity = 2, MinStock = 5,
                Branch = new Branch { Name = "A" }, Dish = new Dish { Name = "Pizza" } },
            new() { BranchId = 2, DishId = Guid.NewGuid(), Quantity = 0, MinStock = 3,
                Branch = new Branch { Name = "B" }, Dish = new Dish { Name = "Pasta" } }
        };
        _branchRepo.Setup(r => r.GetLowStockAcrossNetworkAsync()).ReturnsAsync(stocks);

        var result = (await BuildSut().GetLowStockAlertsAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.IsBelowMinimum.Should().BeTrue());
    }
}
