using FluentAssertions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class DishServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDishRepository> _dishRepo = new();
    private readonly Mock<IGenericRepository<Category>> _catRepo = new();

    public DishServiceTests()
    {
        _uow.Setup(u => u.Dishes).Returns(_dishRepo.Object);
        _uow.Setup(u => u.Categories).Returns(_catRepo.Object);
    }

    private DishService BuildSut() => new(_uow.Object);

    [Fact]
    public async Task DeleteDish_Does_Hard_Delete_When_No_Associated_Orders()
    {
        var dish = new Dish { DishId = Guid.NewGuid(), Name = "Pizza", IsDeleted = false };
        _dishRepo.Setup(r => r.GetByIdAsync(dish.DishId)).ReturnsAsync(dish);
        _dishRepo.Setup(r => r.HasAssociatedOrdersAsync(dish.DishId)).ReturnsAsync(false);

        var ok = await BuildSut().DeleteDishAsync(dish.DishId);

        ok.Should().BeTrue();
        _dishRepo.Verify(r => r.Delete(dish), Times.Once);
        _dishRepo.Verify(r => r.Update(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDish_Does_Soft_Delete_When_Associated_Orders_Exist()
    {
        var dish = new Dish { DishId = Guid.NewGuid(), Name = "Pizza", IsDeleted = false, Available = true };
        _dishRepo.Setup(r => r.GetByIdAsync(dish.DishId)).ReturnsAsync(dish);
        _dishRepo.Setup(r => r.HasAssociatedOrdersAsync(dish.DishId)).ReturnsAsync(true);

        var ok = await BuildSut().DeleteDishAsync(dish.DishId);

        ok.Should().BeTrue();
        dish.IsDeleted.Should().BeTrue();
        dish.Available.Should().BeFalse();
        _dishRepo.Verify(r => r.Delete(It.IsAny<Dish>()), Times.Never);
        _dishRepo.Verify(r => r.Update(dish), Times.Once);
    }

    [Fact]
    public async Task DeleteDish_Returns_False_When_Dish_Not_Found()
    {
        _dishRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((Dish?)null);

        var ok = await BuildSut().DeleteDishAsync(Guid.NewGuid());

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDish_Returns_Null_When_Name_Not_Unique()
    {
        _dishRepo.Setup(r => r.IsNameUniqueAsync("Pizza", null)).ReturnsAsync(false);

        var result = await BuildSut().CreateDishAsync(new DishRequest { Name = "Pizza", Category = 1, Price = 10 });

        result.Should().BeNull();
    }
}
