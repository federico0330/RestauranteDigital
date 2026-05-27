using FluentAssertions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class WarrantyServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IWarrantyClaimRepository> _claimRepo = new();

    public WarrantyServiceTests()
    {
        _uow.Setup(u => u.Orders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.WarrantyClaims).Returns(_claimRepo.Object);
    }

    private WarrantyService BuildSut() => new(_uow.Object);

    [Fact]
    public async Task Create_Returns_Null_When_Order_Does_Not_Exist()
    {
        _orderRepo.Setup(r => r.GetOrderWithDetailsAsync(99)).ReturnsAsync((Order?)null);

        var result = await BuildSut().CreateAsync(new WarrantyClaimRequest(99, 1, "razón"));

        result.Should().BeNull();
        _claimRepo.Verify(r => r.AddAsync(It.IsAny<WarrantyClaim>()), Times.Never);
    }

    [Fact]
    public async Task Create_Returns_Null_When_Item_Does_Not_Belong_To_Order()
    {
        var order = new Order { OrderId = 1, OrderItems = new List<OrderItem> { new() { OrderItemId = 10 } } };
        _orderRepo.Setup(r => r.GetOrderWithDetailsAsync(1)).ReturnsAsync(order);

        var result = await BuildSut().CreateAsync(new WarrantyClaimRequest(1, 999, "x"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Resolve_Approve_Marks_Claim_As_Approved_With_Resolution()
    {
        var claim = new WarrantyClaim
        {
            Id = 5, Status = WarrantyClaimStatus.Pending,
            OrderItem = new OrderItem { Dish = new Dish { Name = "Pizza" } }
        };
        _claimRepo.Setup(r => r.GetWithOrderAsync(5)).ReturnsAsync(claim);

        var result = await BuildSut().ResolveAsync(5, new WarrantyClaimResolveRequest(true, "Reintegrado"));

        result.Should().NotBeNull();
        claim.Status.Should().Be(WarrantyClaimStatus.Approved);
        claim.Resolution.Should().Be("Reintegrado");
        claim.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Resolve_Reject_Marks_Claim_As_Rejected()
    {
        var claim = new WarrantyClaim
        {
            Id = 5, Status = WarrantyClaimStatus.Pending,
            OrderItem = new OrderItem { Dish = new Dish { Name = "Pizza" } }
        };
        _claimRepo.Setup(r => r.GetWithOrderAsync(5)).ReturnsAsync(claim);

        var result = await BuildSut().ResolveAsync(5, new WarrantyClaimResolveRequest(false, "Sin evidencia"));

        result.Should().NotBeNull();
        claim.Status.Should().Be(WarrantyClaimStatus.Rejected);
    }

    [Fact]
    public async Task Resolve_Refuses_To_Re_Resolve_An_Already_Closed_Claim()
    {
        var claim = new WarrantyClaim { Id = 5, Status = WarrantyClaimStatus.Approved };
        _claimRepo.Setup(r => r.GetWithOrderAsync(5)).ReturnsAsync(claim);

        var result = await BuildSut().ResolveAsync(5, new WarrantyClaimResolveRequest(false, "x"));

        result.Should().BeNull();
    }
}
