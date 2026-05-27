using FluentValidation;
using Restaurante.Application.DTOs;

namespace Restaurante.Application.Validators;

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(o => o.DeliveryType).GreaterThan(0);
        RuleFor(o => o.BranchId).GreaterThan(0).WithMessage("Hay que indicar en qué sucursal se toma la orden.");
        RuleFor(o => o.Items).NotEmpty().WithMessage("La orden debe tener al menos un ítem.");
        RuleForEach(o => o.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DishId).NotEqual(Guid.Empty);
        });
    }
}

public class WholesaleOrderRequestValidator : AbstractValidator<WholesaleOrderRequest>
{
    public WholesaleOrderRequestValidator()
    {
        RuleFor(o => o.BranchId).GreaterThan(0);
        RuleFor(o => o.SupplierName).NotEmpty().MaximumLength(255);
        RuleFor(o => o.Items).NotEmpty();
        RuleForEach(o => o.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitCost).GreaterThan(0);
            item.RuleFor(i => i.DishId).NotEqual(Guid.Empty);
        });
    }
}

public class WarrantyClaimRequestValidator : AbstractValidator<WarrantyClaimRequest>
{
    public WarrantyClaimRequestValidator()
    {
        RuleFor(w => w.OrderId).GreaterThan(0);
        RuleFor(w => w.OrderItemId).GreaterThan(0);
        RuleFor(w => w.Reason).NotEmpty().MaximumLength(1000);
    }
}
